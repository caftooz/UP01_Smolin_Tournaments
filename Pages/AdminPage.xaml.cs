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
    /// Логика взаимодействия для AdminPage.xaml
    /// </summary>
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            InitializeComponent();
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || Session.DenyIfNotAdmin(this)) return;

            var db = Db.Ctx;
            TbHello.Text = "Здравствуйте, " + App.CurrentUser.FirstName + "!";
            TbStats.Text = $"В базе: турниров — {db.Tournaments.Count()}, пользователей — {db.Users.Count()}, " +
                           $"дисциплин — {db.Disciplines.Count()}, команд — {db.Teams.Count()}";
        }

        private void BtnTournaments_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Tournaments()));
        private void BtnUsers_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Users()));
        private void BtnDisciplines_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Disciplines()));
        private void BtnFormats_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Formats()));
        private void BtnStatuses_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Statuses()));
        private void BtnVenues_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new TablePage(Tables.Venues()));
        private void BtnCatalog_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new UserPage());
        private void BtnDiagram_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new DiagrammPage());
        private void BtnChangePass_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new ChangePassPage());
    }
}
