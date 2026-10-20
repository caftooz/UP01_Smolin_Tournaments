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
    /// Логика взаимодействия для ChangePassPage.xaml
    /// </summary>
    public partial class ChangePassPage : Page
    {
        public ChangePassPage()
        {
            InitializeComponent();
            InputMask.Login(TbLogin);
            if (App.CurrentUser != null)
                TbLogin.Text = App.CurrentUser.Login;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();

            if (login == "" || PbOld.Password == "" || PbNew.Password == "" || PbNew2.Password == "")
            {
                Msg.Error("Все поля обязательны к заполнению!");
                return;
            }

            User user = AuthService.Authenticate(login, PbOld.Password);
            if (user == null)
            {
                Msg.Error("Текущий пароль или логин неверный!");
                return;
            }

            string error;
            if (!PasswordHelper.Validate(PbNew.Password, out error)) { Msg.Error(error); return; }
            if (PbNew.Password != PbNew2.Password) { Msg.Error("Новые пароли не совпадают!"); return; }
            if (PbNew.Password == PbOld.Password) { Msg.Error("Новый пароль должен отличаться от текущего!"); return; }

            User tracked = Db.Ctx.Users.Find(user.UserID);
            string salt = PasswordHelper.CreateSalt();
            tracked.PasswordSalt = salt;
            tracked.PasswordHash = PasswordHelper.GetHash(PbNew.Password, salt);
            if (!Db.TrySave()) return;

            Audit.Write("PASSWORD", "Users", user.UserID, user.UserID);
            Msg.Info("Пароль успешно изменён!");
            NavigationService?.GoBack();
        }
    }
}
