using System;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public static class TournamentRules
    {
        private static readonly Expression<Func<User, bool>> IsOrganizer =
            u => u.IsActive && (u.Role.Name == Session.OrganizerRole || u.Role.Name == Session.AdminRole);

        public static EntityRules Create()
        {
            var rules = new EntityRules
            {
                OnCreate = entity => SetDefaults((Tournament)entity),
                Validate = (entity, v, ctx) => Validate((Tournament)entity, v, ctx),
                FormReady = LinkOnlineWithVenue
            };
            rules.Choices["VenueID"] = () => Db.Ctx.Venues.Where(x => x.VenueType != "Онлайн").OrderBy(x => x.Name).ToList();
            rules.Choices["OrganizerID"] = () => Db.Ctx.Users.Where(IsOrganizer).ToList().OrderBy(u => u.FullName);
            return rules;
        }

        private static void SetDefaults(Tournament t)
        {
            t.CreatedAt = DateTime.Now;
            t.RegistrationStart = DateTime.Today;
            t.RegistrationEnd = DateTime.Today.AddDays(14);
            t.StartDate = DateTime.Today.AddDays(21).AddHours(12);
            t.MaxTeams = 8;
            t.TournamentStatusID = Db.Ctx.TournamentStatuses.Select(s => s.TournamentStatusID).OrderBy(id => id).FirstOrDefault();
            if (App.CurrentUser != null) t.OrganizerID = App.CurrentUser.UserID;
        }

        private static void Validate(Tournament t, FormValidator v, EditContext ctx)
        {
            if (t.IsOnline) t.VenueID = null;
            else v.Require(t.VenueID != null, "Для офлайн-турнира выберите площадку.");

            v.Require(t.RegistrationStart <= t.RegistrationEnd, "Регистрация не может закончиться раньше, чем начнётся.");
            v.Require(t.RegistrationEnd <= t.StartDate, "Регистрация должна закончиться не позже начала турнира.");
            v.Require(t.EndDate == null || t.EndDate >= t.StartDate, "Дата окончания не может быть раньше даты начала.");

            int organizerId = t.OrganizerID;
            v.Require(organizerId == 0 || Db.Ctx.Users.Where(IsOrganizer).Any(u => u.UserID == organizerId),
                      "Организатором может быть только активный пользователь с ролью «Организатор» или «Администратор».");

            int id = ctx.Id;
            int accepted = ctx.IsNew ? 0 : Db.Ctx.TournamentParticipants.Count(p => p.TournamentID == id);
            v.Require(t.MaxTeams < 2 || t.MaxTeams >= accepted, $"Уже допущено команд: {accepted}. Максимум не может быть меньше.");
        }

        private static void LinkOnlineWithVenue(EditContext ctx)
        {
            var online = (CheckBox)ctx.Controls["IsOnline"];
            var venue = (ComboBox)ctx.Controls["VenueID"];
            RoutedEventHandler sync = (s, e) =>
            {
                venue.IsEnabled = online.IsChecked != true;
                if (online.IsChecked == true) venue.SelectedItem = null;
            };
            online.Checked += sync;
            online.Unchecked += sync;
            sync(null, null);
        }
    }
}
