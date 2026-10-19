using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для RegPage.xaml
    /// </summary>
    public partial class RegPage : Page
    {
        public RegPage()
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

        private void TbLogin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[A-Za-z0-9_]+$");
        }
        private void TbFio_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[А-Яа-яЁё\s\-]+$");
        }

        private void BtnReg_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string fio = Regex.Replace(TbFio.Text.Trim(), @"\s+", " ");
            string pass = PbPass.Password;

            if (login == "" || fio == "" || pass == "" || PbPass2.Password == "")
            {
                Msg.Error("Заполните все поля!");
                return;
            }

            if (!Regex.IsMatch(login, @"^[A-Za-z0-9_]{3,50}$"))
            {
                Msg.Error("Логин: от 3 до 50 символов, только латинские буквы, цифры и «_»!");
                return;
            }
            if (!Regex.IsMatch(fio, @"^[А-ЯЁа-яё\-]+(\s[А-ЯЁа-яё\-]+){1,2}$"))
            {
                Msg.Error("Введите ФИО полностью: фамилия, имя и (при наличии) отчество русскими буквами!");
                return;
            }

            var db = DB_Smolin_TournamentsEntities.GetContext();
            if (db.Users.Any(u => u.Login == login))
            {
                Msg.Error("Пользователь с таким логином уже существует!");
                return;
            }

            string error;
            if (!PasswordHelper.Validate(pass, out error))
            {
                Msg.Error(error);
                return;
            }
            if (pass != PbPass2.Password)
            {
                Msg.Error("Пароли не совпадают!");
                return;
            }

            string[] nameParts = fio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2 || nameParts.Length > 3)
            { Msg.Error("Введите фамилию, имя и необязательно отчество."); return; }
            string salt = PasswordHelper.CreateSalt();
            string roleName = ((ComboBoxItem)CbRole.SelectedItem).Tag.ToString();
            int roleId = db.Roles.Where(r => r.Name == roleName).Select(r => r.RoleID).FirstOrDefault();
            if (roleId == 0) { Msg.Error("Выбранная роль не найдена в справочнике."); return; }
            var user = new User
            {
                Login = login,
                PasswordSalt = salt,
                PasswordHash = PasswordHelper.GetHash(pass, salt),
                LastName = nameParts[0],
                FirstName = nameParts[1],
                MiddleName = nameParts.Length == 3 ? nameParts[2] : null,
                BirthDate = new DateTime(2000, 1, 1),
                Email = login + "@example.local",
                RegisteredAt = DateTime.Now,
                RoleID = roleId,
                IsActive = true
            };
            db.Users.Add(user);
            try
            {
                db.SaveChanges();
                Msg.Info("Пользователь успешно зарегистрирован!");
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                db.Users.Remove(user);
                Msg.Error("Не удалось сохранить: " + ex.Message);
            }
        }
    }
}
