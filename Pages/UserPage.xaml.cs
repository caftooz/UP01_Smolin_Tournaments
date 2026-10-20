using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UP01_Smolin_Tournaments.Helpers;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для UserPage.xaml
    /// </summary>
    public partial class UserPage : Page
    {
        private List<Tournament> _all = new List<Tournament>();
        private bool _ready;

        public UserPage()
        {
            InitializeComponent();
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible) LoadData();
        }

        private void LoadData()
        {
            _ready = false;
            var db = Db.Ctx;

            _all = db.Tournaments
                .Include("Discipline").Include("TournamentFormat").Include("TournamentStatus")
                .Include("Venue").Include("User")
                .ToList();

            int disciplineId = (CbDiscipline.SelectedItem as Discipline)?.DisciplineID ?? 0;
            int statusId = (CbStatus.SelectedItem as TournamentStatus)?.TournamentStatusID ?? 0;

            var disciplines = db.Disciplines.OrderBy(d => d.Name).ToList();
            disciplines.Insert(0, new Discipline { DisciplineID = 0, Name = "Все дисциплины" });
            CbDiscipline.ItemsSource = disciplines;
            CbDiscipline.SelectedItem = disciplines.FirstOrDefault(d => d.DisciplineID == disciplineId) ?? disciplines[0];

            var statuses = db.TournamentStatuses.OrderBy(s => s.TournamentStatusID).ToList();
            statuses.Insert(0, new TournamentStatus { TournamentStatusID = 0, Name = "Все статусы" });
            CbStatus.ItemsSource = statuses;
            CbStatus.SelectedItem = statuses.FirstOrDefault(s => s.TournamentStatusID == statusId) ?? statuses[0];

            if (CbSort.SelectedIndex < 0) CbSort.SelectedIndex = 0;

            TbHello.Text = App.CurrentUser != null ? "Турниры — добро пожаловать, " + App.CurrentUser.FirstName + "!" : "Турниры";

            _ready = true;
            UpdateList();
        }

        private void UpdateList()
        {
            if (!_ready) return;
            IEnumerable<Tournament> list = _all;

            string q = TbSearch.Text.Trim().ToLower();
            if (q.Length > 0)
                list = list.Where(t =>
                    t.Name.ToLower().Contains(q) ||
                    t.Discipline.Name.ToLower().Contains(q) ||
                    t.PlaceText.ToLower().Contains(q) ||
                    (t.User != null && t.User.FullName.ToLower().Contains(q)));

            var discipline = CbDiscipline.SelectedItem as Discipline;
            if (discipline != null && discipline.DisciplineID != 0)
                list = list.Where(t => t.DisciplineID == discipline.DisciplineID);

            var status = CbStatus.SelectedItem as TournamentStatus;
            if (status != null && status.TournamentStatusID != 0)
                list = list.Where(t => t.TournamentStatusID == status.TournamentStatusID);

            if (ChkOnline.IsChecked == true)
                list = list.Where(t => t.IsOnline);

            switch (CbSort.SelectedIndex)
            {
                case 1: list = list.OrderByDescending(t => t.StartDate); break;
                case 2: list = list.OrderBy(t => t.EntryFee); break;
                case 3: list = list.OrderByDescending(t => t.EntryFee); break;
                case 4: list = list.OrderByDescending(t => t.PrizePool); break;
                case 5: list = list.OrderBy(t => t.Name); break;
                default: list = list.OrderBy(t => t.StartDate); break;
            }

            var result = list.ToList();
            ListTournaments.ItemsSource = result;
            TbCount.Text = $"Показано {result.Count} из {_all.Count}";
            if (result.Count == 0 && _all.Count > 0)
                TbCount.Text += " — по заданным условиям ничего не найдено";
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateList();
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateList();
        private void ChkOnline_Changed(object sender, RoutedEventArgs e) => UpdateList();

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _ready = false;
            TbSearch.Clear();
            CbDiscipline.SelectedIndex = 0;
            CbStatus.SelectedIndex = 0;
            CbSort.SelectedIndex = 0;
            ChkOnline.IsChecked = false;
            _ready = true;
            UpdateList();
        }
    }
}
