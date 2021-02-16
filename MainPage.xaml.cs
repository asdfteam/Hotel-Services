using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using HotelLibrary;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Hotel_Services
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class MainPage : Page
    {
        private readonly CoreCursor _coreCursor;
        CoreCursor _cursorBeforePointerEntered = null;
        private Employee employee;

        public MainPage()
        {
            this.InitializeComponent();
            _coreCursor = new CoreCursor(CoreCursorType.Hand, 1);
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

        private void HomeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            switch (button.Name)
            {
                case "ButtonService":
                    employee = new Employee(EmployeeType.ServiceWorker);
                    break;
                case "ButtonCleaner":
                    employee = new Employee(EmployeeType.Cleaner);
                    break;
                case "ButtonMaintenance":
                    employee = new Employee(EmployeeType.Maintainer);
                    break;
            }

            //Navigerer til ny side og sender ref av employee.
            Frame.Navigate(typeof(TaskPage), employee);
        }
    }
}
