using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HotelLibrary;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Hotel_Services
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TaskPage : Page
    {

        private readonly CoreCursor _coreCursor;
        CoreCursor _cursorBeforePointerEntered = null;
        private Employee employee;
        public TaskPage()
        {
            this.InitializeComponent();
            _coreCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Employee parameter)
            {
                employee = parameter;
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
            employee = null;
            Frame.Navigate(typeof(MainPage));
        }

        private void TaskPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hente data her");
        }
    }
}
