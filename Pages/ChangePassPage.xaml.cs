using System.Windows;
using System.Windows.Controls;
using UP01_Smolin_Tournaments.Helpers;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для ChangePassPage.xaml
    /// </summary>
    public partial class ChangePassPage : Page
    {
        public ChangePassPage()
        {
            InitializeComponent();
            if (App.CurrentUser != null)
                TbLogin.Text = App.CurrentUser.Login;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();

            var v = new FormValidator();
            v.Require(login != "" && PbOld.Password != "" && PbNew.Password != "" && PbNew2.Password != "",
                      "Все поля обязательны к заполнению!");
            if (v.ShowErrors()) return;

            User user = AuthService.Authenticate(login, PbOld.Password);
            if (user == null)
            {
                Msg.Error("Текущий пароль или логин неверный!");
                return;
            }

            v.Password(PbNew.Password, PbNew2.Password, "Новые пароли не совпадают!");
            v.Require(PbNew.Password != PbOld.Password, "Новый пароль должен отличаться от текущего!");
            if (v.ShowErrors()) return;

            User tracked = Db.Ctx.Users.Find(user.UserID);
            tracked.SetPassword(PbNew.Password);
            if (!Db.TrySave()) return;

            Audit.Write("PASSWORD", "Users", user.UserID, user.UserID);
            Msg.Info("Пароль успешно изменён!");
            NavigationService?.GoBack();
        }
    }
}
