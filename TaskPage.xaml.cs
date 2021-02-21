using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HotelLibrary;
using Newtonsoft.Json;

/*
 * TODOS:
 * Finish 'Submit' action.
 * Do reload of rooms, maybe async reload with timer?
 * Graphics and colors.
 *
 */

namespace Hotel_Services
{
    using HttpClientImpl;
    public sealed partial class TaskPage : Page
    {
        public string FixedUri = "http://localhost:5000";

        public CoreCursor Cursor { get; }
        public CoreCursor CursorBeforePointerEntered { get; private set; }
        public Employee CurrentEmployee { get; private set; }
        public HttpClient Client { get; } = new HttpClient();
        public HttpClientImpl ClientImpl { get; }
        public List<string> TaskList { get; private set; }
        public string TaskDescriptor { get; set; }
        public List<Room> Rooms { get; set; }
        public Room CurrentRoom { get; set; }

        public TaskPage()
        {
            InitializeComponent();
            Cursor = new CoreCursor(CoreCursorType.Hand, 1);
            ClientImpl = new HttpClientImpl(Client);
            TaskDescriptor = "Looking for tasks...";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Employee parameter)
            {
                CurrentEmployee = parameter;
            }
        }

        private void Button_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            CursorBeforePointerEntered = Window.Current.CoreWindow.PointerCursor;
            Window.Current.CoreWindow.PointerCursor = Cursor;
        }

        private void Button_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = CursorBeforePointerEntered;
        }

        private async void LogoutButton_OnClick(object sender, RoutedEventArgs e)
        {
            var logoutDialog = new ContentDialog
            {
                Title = "Are you sure you want to logout?",
                Content = "Your actions will be saved.",
                PrimaryButtonText = "Log out",
                CloseButtonText = "Cancel"
            };

            var result = await logoutDialog.ShowAsync(); //Venter på brukerklikk

            if (result != ContentDialogResult.Primary) return;
            CurrentEmployee = null;
            CurrentRoom = null;
            Rooms.Clear();
            Frame.Navigate(typeof(MainPage));
        }

        private async void TaskPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            TaskList = ComputeTasks();
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 2);

            var relativeUri = $"/rooms?status={CurrentEmployee.EmployeeType}";
            var response = await ClientImpl.Get(FixedUri + relativeUri);
            if (!response.IsSuccessStatusCode) throw new Exception("Something went wrong...");
            Rooms = TransformHttpContent(response.Content);
            
            Debug.Assert(TaskView.Items != null, "TaskItems.Items == null");
            foreach (var block in Rooms.Select(room => new TextBlock
            {
                Text = $"Room {room.RoomNumber}"
            }))
            {
                TaskView.Items.Add(block);
            }
            TaskView.ItemClick += TaskViewOnViewClick;
            TaskView.Items.VectorChanged += ItemsOnVectorChanged;
            TaskView.PointerEntered += Button_OnPointerEntered;
            TaskView.PointerExited += Button_OnPointerExited;

            TaskDescriptor = (TaskView.Items.Count != 0)
                ? $"Found {TaskView.Items.Count} available tasks"
                : "No available tasks, hooray!";
            Bindings.Update();
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 3);
            
        }

        private void ItemsOnVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
        {
            //Forandre listen på skjermen.
        }

        private void TaskItems_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(TaskView.SelectedItem is TextBlock selected)) return;
            var substr = selected.Text.Substring(5);
            Rooms.ForEach(room =>
            {
                if (room.RoomNumber == int.Parse(substr))
                {
                    CurrentRoom = room;
                }
            });
        }

        private void TaskViewOnViewClick(object sender, ItemClickEventArgs e)
        {
            Subtasks.Children.Clear();
            foreach (var task in TaskList)
            {
                var subStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0,5,0,0)
                };
                var subtask = new TextBlock
                {
                    Text = task,
                    FontSize = 24,
                };
                var checktask = new CheckBox
                {
                    IsChecked = false,
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0,0,0),
                };
                subStackPanel.Children.Add(subtask);
                subStackPanel.Children.Add(checktask);
                Subtasks.Children.Add(subStackPanel);
            }

            var submitButton = new Button
            {
                Content = "Submit",
                Width = 150,
                Height = 50,
                Background = new SolidColorBrush(Colors.White),
                FontSize = 24,
                Margin = new Thickness(0, 20, 0, 0),
                BorderBrush = new SolidColorBrush(Colors.Black),
                Flyout = new Flyout
                {
                    Placement = FlyoutPlacementMode.RightEdgeAlignedBottom,
                    Content = new TextBlock {Text = "Not all tasks are completed..."},
                },
            };
            submitButton.PointerEntered += Button_OnPointerEntered;
            submitButton.PointerExited += Button_OnPointerExited;
            submitButton.Click += SubmitButtonOnClick;
            Subtasks.Children.Add(submitButton);
        }

        private async void SubmitButtonOnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (!VerifyCheckmarks()) return;
            button?.Flyout?.Hide();
            var submitDialog = new ContentDialog
            {
                Title = "Submit changes",
                Content = "Are you sure you want to submit?",
                PrimaryButtonText = "Submit",
                SecondaryButtonText = "Cancel"
            };
            var result = await submitDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;
            CurrentRoom.RoomStatus = "AVAILABLE";

            var relativeUri = $"/rooms/{CurrentRoom.RoomNumber}?newStatus={CurrentRoom.RoomStatus}";
            var response = await ClientImpl.Put(FixedUri + relativeUri);
            //responseting her =)
        }

        private bool VerifyCheckmarks()
        {
            var checkBoxes = Subtasks.Children.OfType<StackPanel>();
            foreach (var stackPanel in checkBoxes)
            {
                foreach (var stackPanelChild in stackPanel.Children)
                {
                    if (stackPanelChild is CheckBox checkbox && checkbox.IsChecked == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private List<string> ComputeTasks()
        {
            string task1;
            string task2;
            string task3;
            var tasklist = new List<string>();
            switch (CurrentEmployee.EmployeeType)
            {
                case "SERVICEWORKER":
                    task1 = "Delivered food and drinks at the current room";
                    tasklist = new List<string> {task1};
                    break;
                case "CLEANER":
                    task1 = "Clean bathroom";
                    task2 = "Vacuum the carpet floor";
                    task3 = "Change the sheets";
                    tasklist = new List<string> {task1, task2, task3};
                    break;
                case "MAINTAINER":
                    task1 = "Check for damages in bathroom";
                    task2 = "Check for damages in living room";
                    task3 = "Check the airconditiong";
                    tasklist = new List<string> {task1, task2, task3};
                    break;
                case "FRONTDESKWORKER":
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return tasklist;
        }

        private static List<Room> TransformHttpContent(HttpContent content)
        {
            var scontent = content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Room>>(scontent.Result);
        } 
    }
}
