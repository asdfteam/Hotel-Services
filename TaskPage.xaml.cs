using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
 * Notes on rooms.
 * Graphics and colors.
 *
 */

namespace Hotel_Services
{
    using HttpClientImpl;
    public sealed partial class TaskPage : Page
    {
        public string FixedUri = "http://localhost:5000";

        public CoreCursor HandCursor { get; }
        public CoreCursor LoadCursor { get; }
        public CoreCursor PointerCursor { get; }
        public Employee CurrentEmployee { get; private set; }
        public HttpClient Client { get; } = new HttpClient();
        public HttpClientImpl ClientImpl { get; }
        public List<string> TaskList { get; private set; }
        public string TaskDescriptor { get; set; }
        public string NoteContainer { get; set; }
        public string StatusContainer { get; set; }
        public List<Room> Rooms { get; set; }
        public Room CurrentRoom { get; set; }
        public Timer RequestTimer { get; }
        public TaskPage()
        {
            NoteContainer = null;
            StatusContainer = null;
            InitializeComponent();
            PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            HandCursor = new CoreCursor(CoreCursorType.Hand, 1);
            LoadCursor = new CoreCursor(CoreCursorType.Wait, 2);
            ClientImpl = new HttpClientImpl(Client);
            RequestTimer = new Timer(TimerCallback, null, (int) TimeSpan.FromSeconds(30).TotalMilliseconds,
                Timeout.Infinite);
            TaskDescriptor = "Looking for tasks...";
        }

        private async void TimerCallback(object state)
        {
            Rooms = await GetRoomsRest();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, UpdateTaskView);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Employee parameter)
            {
                CurrentEmployee = parameter;
            }
        }

        private void OnPointerEnteredEventHandler(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = HandCursor;
        }

        private void OnPointerExitedEventHandler(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = PointerCursor;
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
            RequestTimer.Dispose();
            CurrentEmployee = null;
            CurrentRoom = null;
            Rooms.Clear();
            Frame.Navigate(typeof(MainPage));
        }

        private async void TaskPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            TaskList = ComputeTasks();
            Rooms = await GetRoomsRest();
            UpdateTaskView();

            TaskView.ItemClick += TaskViewOnViewClick;
            TaskView.PointerEntered += OnPointerEnteredEventHandler;
            TaskView.PointerExited += OnPointerExitedEventHandler;
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
                checktask.PointerEntered += OnPointerEnteredEventHandler;
                checktask.PointerExited += OnPointerExitedEventHandler;
                subStackPanel.Children.Add(subtask);
                subStackPanel.Children.Add(checktask);
                Subtasks.Children.Add(subStackPanel);
            }

            var statusRolldown = new ComboBox
            {
                Text = "Choose a new status:",
                Items = { "AVAILABLE", "CLEANING", "MAINTENANCE", "SERVICE", "BUSY" }
            };
            statusRolldown.PointerExited += OnPointerExitedEventHandler;
            statusRolldown.PointerEntered += OnPointerEnteredEventHandler;
            statusRolldown.LostFocus += StatusRolldownOnLostFocus;
            statusRolldown.SelectionChanged += StatusRolldownOnSelectionChanged;
            var notetask = new TextBox
            {
                PlaceholderText = "If you have any comments, please put them here:",
            };
            notetask.TextChanged += NotetaskOnTextChanged;
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
            submitButton.PointerEntered += OnPointerEnteredEventHandler;
            submitButton.PointerExited += OnPointerExitedEventHandler;
            submitButton.Click += SubmitButtonOnClick;
            Subtasks.Children.Add(statusRolldown);
            Subtasks.Children.Add(notetask);
            Subtasks.Children.Add(submitButton);
        }

        private void StatusRolldownOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox dropdown) StatusContainer = dropdown.SelectedItem as string;
        }

        private void StatusRolldownOnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox dropdown) dropdown.IsDropDownOpen = false;
        }

        private void NotetaskOnTextChanged(object sender, TextChangedEventArgs e)
        {
            var b = sender as TextBox;
            NoteContainer = b?.Text;
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
            submitDialog.PointerEntered += OnPointerExitedEventHandler;
            submitDialog.PointerExited += OnPointerExitedEventHandler;
            var result = await submitDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var response = await PutRoomRest();

            if (response)
            {
                //Give user response that it was successful.
                CurrentRoom = null;
                Subtasks.Children.Clear();
                Rooms = await GetRoomsRest();
                UpdateTaskView();
            }
            else
            {
                //Not successful
            }
        }

        private void UpdateTaskView()
        {
            Window.Current.CoreWindow.PointerCursor = LoadCursor;
            Debug.Assert(TaskView.Items != null, "TaskItems.Items == null");
            TaskView.Items.Clear();
            foreach (var block in Rooms.Select(room => new TextBlock
            {
                Text = $"Room {room.RoomNumber}"
            }))
            {
                TaskView.Items.Add(block);
            }
            TaskDescriptor = (TaskView.Items.Count != 0)
                ? $"Found {TaskView.Items.Count} available tasks"
                : "No available tasks, hooray!";
            Bindings.Update();
            Window.Current.CoreWindow.PointerCursor = PointerCursor;
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

        private async Task<List<Room>> GetRoomsRest()
        {
            var relativeUri = $"/rooms?type={CurrentEmployee.EmployeeType}";
            var response = await ClientImpl.Get(FixedUri + relativeUri);
            if (!response.IsSuccessStatusCode) throw new Exception(response.StatusCode.ToString());
            return TransformHttpContent(response.Content);
        }

        private async Task<bool> PutRoomRest()
        {
            var relativeUri = $"/rooms/{CurrentRoom.RoomNumber}?newStatus={StatusContainer}&note={NoteContainer}";
            var response = await ClientImpl.Put(FixedUri + relativeUri);
            return response.IsSuccessStatusCode;
        }
    }
}
