using System.Windows.Controls;
using UP01_Smolin_Tournaments.Pages;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Session
    {
        public const string AdminRole = "Администратор";
        public const string OrganizerRole = "Организатор";
        public const string PlayerRole = "Игрок";

        public static bool IsAdmin =>
            App.CurrentUser != null && App.CurrentUser.Role != null && App.CurrentUser.Role.Name == AdminRole;

        public static bool DenyIfNotAdmin(Page page)
        {
            if (IsAdmin) return false;
            Msg.Error("Недостаточно прав: страница доступна только администратору.");
            page.NavigationService?.Navigate(new AuthPage());
            return true;
        }
    }
}
