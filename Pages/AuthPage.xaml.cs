using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UP01_Smolin_Tournaments.Helpers;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private int _failedAttempts = 0;

        public AuthPage()
        {
            InitializeComponent();
        }

        private bool CaptchaActive => CaptchaPanel.Visibility == Visibility.Visible;

        private void ShowCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Visible;
            RefreshCaptcha();
        }

        private void RefreshCaptcha()
        {
            TbCaptcha.Text = CaptchaHelper.Generate();
            TbCaptchaInput.Clear();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => RefreshCaptcha();

        private void Captcha_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
                e.Handled = true;
        }

        private void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            bool captchaOk = !CaptchaActive || TbCaptchaInput.Text == TbCaptcha.Text;
            var v = new FormValidator();
            v.Require(!string.IsNullOrWhiteSpace(TbLogin.Text) && PbPass.Password.Length > 0, "Введите логин и пароль!");
            v.Require(captchaOk, "Неверно введена капча!");
            if (v.ShowErrors())
            {
                if (!captchaOk) RefreshCaptcha();
                return;
            }

            User user = AuthService.Authenticate(TbLogin.Text.Trim(), PbPass.Password);
            if (user == null)
            {
                _failedAttempts++;
                Msg.Error("Пользователь с такими данными не найден!");
                if (_failedAttempts >= 3)
                    ShowCaptcha();            
                return;
            }

            App.CurrentUser = user;
            AuthService.RegisterLogin(user.UserID);
            Msg.Info($"Добро пожаловать, {user.FullName}!");

            if (Session.IsAdmin)
                NavigationService?.Navigate(new AdminPage());
            else
                NavigationService?.Navigate(new UserPage());
        }

        private void BtnReg_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new RegPage());
    }
}
