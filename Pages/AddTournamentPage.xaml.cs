using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Логика взаимодействия для AddTournamentPage.xaml
    /// </summary>
    public partial class AddTournamentPage : Page
    {
        private readonly int? _id; 

        public AddTournamentPage(int? id)
        {
            InitializeComponent();
            _id = id;

            Title = id == null ? "Добавление турнира" : "Редактирование турнира";
            TbHeader.Text = Title;

            InputMask.Digits(TbMaxTeams);
            InputMask.Money(TbFee, TbPrize);
            InputMask.Time(TbStartTime);

            LoadLists();
            if (id == null) FillDefaults();
            else FillFromDb(id.Value);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Session.DenyIfNotAdmin(this);

        private void LoadLists()
        {
            var db = Db.Ctx;
            CbDiscipline.ItemsSource = db.Disciplines.OrderBy(x => x.Name).ToList();
            CbFormat.ItemsSource = db.TournamentFormats.OrderBy(x => x.Name).ToList();
            CbStatus.ItemsSource = db.TournamentStatuses.OrderBy(x => x.TournamentStatusID).ToList();
            CbVenue.ItemsSource = db.Venues.Where(v => v.VenueType != "Онлайн").OrderBy(x => x.Name).ToList();

            CbOrganizer.ItemsSource = db.Users.Include("Role")
                .Where(u => u.IsActive && (u.Role.Name == Session.OrganizerRole || u.Role.Name == Session.AdminRole))
                .ToList()                     
                .OrderBy(u => u.FullName)
                .ToList();
        }

        private void FillDefaults()
        {
            DpRegStart.SelectedDate = DateTime.Today;
            DpRegEnd.SelectedDate = DateTime.Today.AddDays(14);
            DpStart.SelectedDate = DateTime.Today.AddDays(21);
            TbStartTime.Text = "12:00";
            TbMaxTeams.Text = "8";
            TbFee.Text = "0";
            TbPrize.Text = "0";
            CbStatus.SelectedIndex = 0;
            if (App.CurrentUser != null) CbOrganizer.SelectedValue = App.CurrentUser.UserID;
        }

        private void FillFromDb(int id)
        {
            Tournament t = Db.Ctx.Tournaments.Find(id);
            if (t == null) { Msg.Error("Турнир не найден — возможно, он уже удалён."); return; }

            TbName.Text = t.Name;
            CbDiscipline.SelectedValue = t.DisciplineID;
            CbFormat.SelectedValue = t.TournamentFormatID;
            CbStatus.SelectedValue = t.TournamentStatusID;
            CbOrganizer.SelectedValue = t.OrganizerID;
            ChkOnline.IsChecked = t.IsOnline;
            CbVenue.SelectedValue = t.VenueID;
            DpRegStart.SelectedDate = t.RegistrationStart.Date;
            DpRegEnd.SelectedDate = t.RegistrationEnd.Date;
            DpStart.SelectedDate = t.StartDate.Date;
            TbStartTime.Text = t.StartDate.ToString("HH:mm");
            DpEnd.SelectedDate = t.EndDate?.Date;
            TbMaxTeams.Text = t.MaxTeams.ToString();
            TbFee.Text = t.EntryFee.ToString("0.##");
            TbPrize.Text = t.PrizePool.ToString("0.##");
            TbDescription.Text = t.Description;
            TbRules.Text = t.Rules;
        }

        private void ChkOnline_Changed(object sender, RoutedEventArgs e)
        {
            bool online = ChkOnline.IsChecked == true;
            CbVenue.IsEnabled = !online;
            if (online) CbVenue.SelectedItem = null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var v = new FormValidator();

            string name = v.Text(TbName.Text, "Название", 3, 150);
            v.Require(CbDiscipline.SelectedItem != null, "Выберите дисциплину.");
            v.Require(CbFormat.SelectedItem != null, "Выберите формат розыгрыша.");
            v.Require(CbStatus.SelectedItem != null, "Выберите статус.");
            v.Require(CbOrganizer.SelectedItem != null, "Выберите организатора.");
            bool online = ChkOnline.IsChecked == true;
            v.Require(online || CbVenue.SelectedItem != null, "Для офлайн-турнира выберите площадку.");

            int maxTeams = v.Int(TbMaxTeams.Text, "Максимум команд", 2, 128);
            decimal fee = v.Money(TbFee.Text, "Взнос", 0, 100000);
            decimal prize = v.Money(TbPrize.Text, "Призовой фонд", 0, 10000000);
            string description = v.Text(TbDescription.Text, "Описание", 0, 1000, required: false);
            string rules = v.Text(TbRules.Text, "Регламент", 0, 4000, required: false);

            v.Require(DpRegStart.SelectedDate != null, "Укажите дату начала регистрации.");
            v.Require(DpRegEnd.SelectedDate != null, "Укажите дату окончания регистрации.");
            v.Require(DpStart.SelectedDate != null, "Укажите дату начала турнира.");
            TimeSpan time;
            bool timeOk = TimeSpan.TryParseExact(TbStartTime.Text.Trim(), new[] { @"hh\:mm", @"h\:mm" },
                                                 CultureInfo.InvariantCulture, out time);
            v.Require(timeOk, "Время начала укажите в формате чч:мм, например 18:30.");

            DateTime start = DateTime.MinValue;
            if (DpRegStart.SelectedDate != null && DpRegEnd.SelectedDate != null && DpStart.SelectedDate != null && timeOk)
            {
                start = DpStart.SelectedDate.Value.Date + time;
                v.Require(DpRegStart.SelectedDate.Value <= DpRegEnd.SelectedDate.Value,
                          "Регистрация не может закончиться раньше, чем начнётся.");
                v.Require(DpRegEnd.SelectedDate.Value <= start,
                          "Регистрация должна закончиться не позже начала турнира.");
                v.Require(DpEnd.SelectedDate == null || DpEnd.SelectedDate.Value.Date >= start.Date,
                          "Дата окончания не может быть раньше даты начала.");
            }

            if (_id != null && maxTeams >= 2)
            {
                int id = _id.Value;
                int accepted = Db.Ctx.TournamentParticipants.Count(p => p.TournamentID == id);
                v.Require(maxTeams >= accepted, $"Уже допущено команд: {accepted}. Максимум не может быть меньше.");
            }

            if (v.ShowErrors()) return;

            var db = Db.Ctx;
            Tournament t;
            if (_id == null)
            {
                t = new Tournament { CreatedAt = DateTime.Now };  
                db.Tournaments.Add(t);
            }
            else
            {
                t = db.Tournaments.Find(_id.Value);
                if (t == null) { Msg.Error("Турнир не найден — возможно, его уже удалили."); return; }
            }

            t.Name = name;
            t.DisciplineID = (int)CbDiscipline.SelectedValue;
            t.TournamentFormatID = (int)CbFormat.SelectedValue;
            t.TournamentStatusID = (int)CbStatus.SelectedValue;
            t.OrganizerID = (int)CbOrganizer.SelectedValue;
            t.IsOnline = online;
            t.VenueID = online ? (int?)null : (int)CbVenue.SelectedValue;

            t.RegistrationStart = KeepTime(t.RegistrationStart, DpRegStart.SelectedDate.Value);
            t.RegistrationEnd = KeepTime(t.RegistrationEnd, DpRegEnd.SelectedDate.Value);
            t.StartDate = start;
            if (DpEnd.SelectedDate == null)
                t.EndDate = null;
            else if (t.EndDate == null || t.EndDate.Value.Date != DpEnd.SelectedDate.Value.Date)
                t.EndDate = DpEnd.SelectedDate.Value.Date.AddHours(23).AddMinutes(59);   
            t.MaxTeams = maxTeams;
            t.EntryFee = fee;
            t.PrizePool = prize;
            t.Description = description;
            t.Rules = rules;

            if (!Db.TrySave()) return;

            Audit.Write(_id == null ? "INSERT" : "UPDATE", "Tournaments", t.TournamentID);
            Msg.Info("Турнир сохранён.");
            NavigationService?.GoBack();
        }

        private static DateTime KeepTime(DateTime old, DateTime picked) =>
            old.Date == picked.Date ? old : picked.Date;

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}
