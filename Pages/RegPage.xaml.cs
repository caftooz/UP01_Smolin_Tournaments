using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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

        private void BtnReg_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string fio = Regex.Replace(TbFio.Text.Trim(), @"\s+", " ");
            string pass = PbPass.Password;
            var db = Db.Ctx;

            var v = new FormValidator();
            v.Require(login != "" && fio != "" && pass != "" && PbPass2.Password != "", "Заполните все поля!");
            if (login != "")
            {
                bool loginOk = Regex.IsMatch(login, @"^[A-Za-z0-9_]{3,50}$");
                v.Require(loginOk, "Логин: от 3 до 50 символов, только латинские буквы, цифры и «_»!");
                if (loginOk) v.Require(!db.Users.Any(u => u.Login == login), "Пользователь с таким логином уже существует!");
            }
            if (fio != "")
                v.Require(Regex.IsMatch(fio, @"^[А-ЯЁа-яё\-]+(\s[А-ЯЁа-яё\-]+){1,2}$"),
                          "Введите ФИО полностью: фамилия, имя и (при наличии) отчество русскими буквами!");
            if (pass != "")
                v.Password(pass, PbPass2.Password, "Пароли не совпадают!");
            if (v.ShowErrors()) return;

            string roleName = ((ComboBoxItem)CbRole.SelectedItem).Tag.ToString();
            int roleId = db.Roles.Where(r => r.Name == roleName).Select(r => r.RoleID).FirstOrDefault();
            if (roleId == 0) { Msg.Error("Выбранная роль не найдена в справочнике."); return; }

            string[] nameParts = fio.Split(' ');
            var user = new User
            {
                Login = login,
                LastName = nameParts[0],
                FirstName = nameParts[1],
                MiddleName = nameParts.Length == 3 ? nameParts[2] : null,
                BirthDate = new DateTime(2000, 1, 1),
                Email = login + "@example.local",
                RegisteredAt = DateTime.Now,
                RoleID = roleId,
                IsActive = true
            };
            user.SetPassword(pass);
            db.Users.Add(user);
            if (!Db.TrySave()) return;

            Msg.Info("Пользователь успешно зарегистрирован!");
            NavigationService?.GoBack();
        }
    }
}
