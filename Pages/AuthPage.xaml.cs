using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

        private void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            Ui.UpdatePlaceholder(box, box.Text.Length > 0);
        }
        private void Pb_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = (PasswordBox)sender;
            Ui.UpdatePlaceholder(box, box.Password.Length > 0);
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
            if (string.IsNullOrWhiteSpace(TbLogin.Text) || string.IsNullOrEmpty(PbPass.Password))
            {
                Msg.Error("Введите логин и пароль!");
                return;
            }

            if (CaptchaActive && TbCaptchaInput.Text != TbCaptcha.Text)
            {
                Msg.Error("Неверно введена капча!");
                RefreshCaptcha();
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
            string fio = string.Join(" ", new[] { user.LastName, user.FirstName, user.MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            Msg.Info($"Добро пожаловать, {fio}!");

            if (user.Role.Name == "Администратор")
                NavigationService?.Navigate(new AdminPage());
            else
                NavigationService?.Navigate(new UserPage());
        }

        private void BtnReg_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new RegPage());

        private void BtnChangePass_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new ChangePassPage());
    }
}
