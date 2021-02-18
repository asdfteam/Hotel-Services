using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HotelLibrary;
using Newtonsoft.Json;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Hotel_Services
{
    using HttpClientImpl;
    public sealed partial class TaskPage : Page
    {
        public string FixedUri = "http://localhost:5000";

        private readonly CoreCursor _coreCursor;
        CoreCursor _cursorBeforePointerEntered;
        public Employee CurrentEmployee { get; private set; }
        private readonly HttpClient _httpClient = new HttpClient();
        public HttpClientImpl ClientImpl { get; }
        public List<string> TaskList { get; private set; }


        public string TaskDescriptor { get; set; }
        public List<Room> Rooms { get; set; }
        public Room CurrentRoom { get; set; }

        public TaskPage()
        {
            this.InitializeComponent();
            _coreCursor = new CoreCursor(CoreCursorType.Hand, 1);
            ClientImpl = new HttpClientImpl(_httpClient);

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
            _cursorBeforePointerEntered = Window.Current.CoreWindow.PointerCursor;
            Window.Current.CoreWindow.PointerCursor = _coreCursor;
        }


        private void Button_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = _cursorBeforePointerEntered;
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
            Frame.Navigate(typeof(MainPage));
        }

        private void TaskPage_OnLoaded(object sender, RoutedEventArgs e)
        {

            TaskList = ComputeTasks();
            /*
            _httpClientImpl = new HttpClientImpl(_httpClient);
            var relativeuri = "/service/rooms/" + _employee.EmployeeType.ToString();
            var response = await _httpClientImpl.Get(FixedUri + relativeuri);
             */
            var room1 = new Room(101, 1, 1);
            room1.SetStatus(RoomStatus.Maintenance);
            var room2 = new Room(102, 1, 1);
            room2.SetStatus(RoomStatus.Cleaning);
            var room3 = new Room(103, 1, 1);
            room3.SetStatus(RoomStatus.Busy);
            var room4 = new Room(104, 1, 1);
            room4.SetStatus(RoomStatus.Maintenance);
            var room5 = new Room(105, 1, 1);
            room5.SetStatus(RoomStatus.Cleaning);
            var room6 = new Room(106, 1, 1);
            room6.SetStatus(RoomStatus.Service);
            var room7 = new Room(107, 1, 1);
            Rooms = new List<Room>
            {
                room1,
                room2,
                room3,
                room4,
                room5,
                room6,
                room7
            };

            var identifyStatus = RoomStatus.Available;

            switch (CurrentEmployee.EmployeeType)
            {
                case EmployeeType.Cleaner:
                    identifyStatus = RoomStatus.Cleaning;
                    break;
                case EmployeeType.Maintainer:
                    identifyStatus = RoomStatus.Maintenance;
                    break;
                case EmployeeType.ServiceWorker:
                    identifyStatus = RoomStatus.Service;
                    break;
                case EmployeeType.FrontDeskWorker:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Debug.Assert(TaskView.Items != null, "TaskItems.Items != null");
            foreach (var room in Rooms.Where(room => room.Status == identifyStatus))
            {
                var block = new TextBlock
                {
                    Text = $"Room {room.RoomNumber}"
                };
                
                TaskView.Items.Add(block);
            }
            TaskView.ItemClick += TaskViewOnViewClick;
            TaskView.Items.VectorChanged += ItemsOnVectorChanged;

            //Debug
            TaskDescriptor = (TaskView.Items.Count != 0)
                ? $"Found {TaskView.Items.Count} available tasks"
                : "No available tasks, hooray!";
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
            CurrentRoom.Status = RoomStatus.Available;
            var relativeUri = $"/service/rooms/{CurrentRoom.RoomNumber}";
            //var response = ClientImpl.Put(FixedUri + relativeUri, JsonConvert.SerializeObject(CurrentRoom));
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
                case EmployeeType.ServiceWorker:
                    task1 = "Delivered food and drinks at the current room";
                    tasklist = new List<string> {task1};
                    break;
                case EmployeeType.Cleaner:
                    task1 = "Clean bathroom";
                    task2 = "Vacuum the carpet floor";
                    task3 = "Change the sheets";
                    tasklist = new List<string> {task1, task2, task3};
                    break;
                case EmployeeType.Maintainer:
                    task1 = "Check for damages in bathroom";
                    task2 = "Check for damages in living room";
                    task3 = "Check the airconditiong";
                    tasklist = new List<string> {task1, task2, task3};
                    break;
            }
            return tasklist;
        }
    }
}
