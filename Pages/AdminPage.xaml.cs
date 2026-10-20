using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private void Section_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(new TablePage((Type)((Button)sender).Tag));

        private void BtnCatalog_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new UserPage());
        private void BtnDiagram_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new DiagrammPage());
    }
}
