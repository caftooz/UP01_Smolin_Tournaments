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
    /// Логика взаимодействия для AddUserPage.xaml
    /// </summary>
    public partial class AddUserPage : Page
    {
        private readonly int? _id;
        private const string NamePattern = @"^[А-ЯЁа-яё]+(-[А-ЯЁа-яё]+)?$";

        public AddUserPage(int? id)
        {
            InitializeComponent();
            _id = id;
            Title = id == null ? "Добавление пользователя" : "Редактирование пользователя";
            TbHeader.Text = Title;

            InputMask.Login(TbLogin);
            InputMask.Cyrillic(TbLastName, TbFirstName, TbMiddleName);
            InputMask.Phone(TbPhone);

            CbRole.ItemsSource = Db.Ctx.Roles.OrderBy(r => r.AccessLevel).ToList();

            if (id == null)
            {
                CbRole.SelectedIndex = 1;  
                return;
            }

            TbPassLabel.Text = "Новый пароль (оставьте пустым, чтобы не менять)";

            User u = Db.Ctx.Users.Find(id.Value);
            if (u == null) { Msg.Error("Пользователь не найден — возможно, он уже удалён."); return; }
            TbLogin.Text = u.Login;
            TbLastName.Text = u.LastName;
            TbFirstName.Text = u.FirstName;
            TbMiddleName.Text = u.MiddleName;
            DpBirth.SelectedDate = u.BirthDate;
            TbEmail.Text = u.Email;
            TbPhone.Text = u.Phone;
            CbRole.SelectedValue = u.RoleID;
            ChkActive.IsChecked = u.IsActive;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Session.DenyIfNotAdmin(this);

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var v = new FormValidator();
            int selfId = _id ?? 0;
            bool editingMyself = App.CurrentUser != null && selfId == App.CurrentUser.UserID;

            string login = v.Text(TbLogin.Text, "Логин", 3, 50);
            if (login != null)
            {
                v.Require(Regex.IsMatch(login, @"^[A-Za-z0-9_]+$"), "Логин: только латинские буквы, цифры и «_».");
                v.Require(!Db.Ctx.Users.Any(u => u.Login == login && u.UserID != selfId), "Такой логин уже занят.");
            }

            string last = v.Text(TbLastName.Text, "Фамилия", 2, 50);
            string first = v.Text(TbFirstName.Text, "Имя", 2, 50);
            string middle = v.Text(TbMiddleName.Text, "Отчество", 2, 50, required: false);
            if (last != null) v.Require(Regex.IsMatch(last, NamePattern), "Фамилия: только русские буквы и дефис.");
            if (first != null) v.Require(Regex.IsMatch(first, NamePattern), "Имя: только русские буквы и дефис.");
            if (middle != null) v.Require(Regex.IsMatch(middle, NamePattern), "Отчество: только русские буквы и дефис.");

            DateTime? birth = DpBirth.SelectedDate;
            v.Require(birth != null, "Укажите дату рождения.");
            if (birth != null)
                v.Require(birth.Value <= DateTime.Today.AddYears(-10) && birth.Value >= DateTime.Today.AddYears(-100),
                          "Возраст пользователя должен быть от 10 до 100 лет.");

            string email = v.Text(TbEmail.Text, "E-mail", 6, 100);
            if (email != null)
            {
                v.Require(Regex.IsMatch(email, @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$"),
                          "E-mail введите в формате name@mail.ru.");
                v.Require(!Db.Ctx.Users.Any(u => u.Email == email && u.UserID != selfId), "Такой e-mail уже используется.");
            }
            string phone = v.Text(TbPhone.Text, "Телефон", 18, 18, required: false);
            if (phone != null)
                v.Require(Regex.IsMatch(phone, @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$"),
                          "Телефон введите в формате +7 (900) 123-45-67.");

            v.Require(CbRole.SelectedItem != null, "Выберите роль.");
            var role = CbRole.SelectedItem as Role;
            bool active = ChkActive.IsChecked == true;
            if (editingMyself)
            {
                v.Require(active, "Нельзя заблокировать собственную учётную запись.");
                v.Require(role == null || role.Name == Session.AdminRole, "Нельзя снять с себя роль администратора.");
            }

            string pass = PbPass.Password;
            bool changePassword = _id == null || pass.Length > 0 || PbPass2.Password.Length > 0;
            if (changePassword)
            {
                string error;
                if (!PasswordHelper.Validate(pass, out error)) v.Require(false, error);
                v.Require(pass == PbPass2.Password, "Пароли не совпадают.");
            }

            if (v.ShowErrors()) return;

            var db = Db.Ctx;
            User item;
            if (_id == null)
            {
                item = new User { RegisteredAt = DateTime.Now };  
                db.Users.Add(item);
            }
            else
            {
                item = db.Users.Find(_id.Value);
                if (item == null) { Msg.Error("Пользователь не найден — возможно, его уже удалили."); return; }
            }

            item.Login = login;
            item.LastName = last;
            item.FirstName = first;
            item.MiddleName = middle;
            item.BirthDate = birth.Value;
            item.Email = email;
            item.Phone = phone;
            item.RoleID = role.RoleID;
            item.IsActive = active;
            if (changePassword)
            {
                string salt = PasswordHelper.CreateSalt();
                item.PasswordSalt = salt;
                item.PasswordHash = PasswordHelper.GetHash(pass, salt);
            }

            if (!Db.TrySave()) return;
            Audit.Write(_id == null ? "INSERT" : "UPDATE", "Users", item.UserID);

            if (editingMyself)
                App.CurrentUser = Db.Ctx.Users.Include("Role").AsNoTracking().First(u => u.UserID == item.UserID);

            Msg.Info("Пользователь сохранён.");
            NavigationService?.GoBack();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}
