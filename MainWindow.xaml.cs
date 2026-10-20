using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using UP01_Smolin_Tournaments.Helpers;
using UP01_Smolin_Tournaments.Pages;

namespace UP01_Smolin_Tournaments
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ThemeManager.Load();
            InitializeComponent();
            MainFrame.Navigate(new AuthPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (o, t) => DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            timer.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!Msg.Confirm("Вы действительно хотите закрыть приложение?"))
                e.Cancel = true;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack) MainFrame.GoBack();
        }

        private void BtnChangePass_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ChangePassPage());
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser == null)
            {
                Msg.Info("Вы ещё не вошли в систему.");
                return;
            }
            if (Msg.Confirm("Вы действительно хотите выйти из учётной записи?"))
            {
                App.CurrentUser = null;
                MainFrame.Navigate(new AuthPage());
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = MainFrame.Content as Page;
            if (page == null) return;

            Title = "Турниры — " + page.Title;

            UserInfo.Text = App.CurrentUser == null
                ? "Гость"
                : $"{App.CurrentUser.FullName} ({App.CurrentUser.Role.Name})";

            bool isHome = page is AuthPage || page is AdminPage
                          || (page is UserPage && App.CurrentUser != null && !Session.IsAdmin);
            if (isHome)
                while (MainFrame.CanGoBack) MainFrame.RemoveBackEntry();

            BtnBack.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            BtnChangePass.Visibility = page is ChangePassPage ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
