using System;
using System.Globalization;
using System.Linq;
using UP01_Smolin_Tournaments.Helpers;

namespace UP01_Smolin_Tournaments
{
    public partial class DB_Smolin_TournamentsEntities
    {
        private static DB_Smolin_TournamentsEntities _context;

        public static DB_Smolin_TournamentsEntities GetContext()
        {
            if (_context == null)
                _context = new DB_Smolin_TournamentsEntities();
            return _context;
        }

        public static void ResetContext()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }

    public partial class Tournament
    {
        private static readonly CultureInfo Ru = new CultureInfo("ru-RU");

        public decimal PlannedEntryRevenue => EntryFee * MaxTeams;
        public string PlaceText => IsOnline ? "Онлайн" : (Venue != null ? Venue.Name : "—");

        public string DayText => StartDate.Day.ToString();
        public string MonthText => StartDate.ToString("MMM", Ru).TrimEnd('.').ToUpper(Ru);
        public string TimeText => StartDate.ToString("dddd, HH:mm", Ru);

        /// <summary>Короткое обозначение игры для плашки: Counter-Strike 2 → CS2, Rocket League → RL.</summary>
        public string DisciplineShort
        {
            get
            {
                string name = Discipline != null ? Discipline.Name : "";
                string letters = new string(name
                    .Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => char.IsLetter(w[0]))
                    .Select(w => char.ToUpper(w[0]))
                    .ToArray());
                string digits = new string(name.Where(char.IsDigit).ToArray());
                string result = letters + digits;
                if (result.Length == 0) return "?";
                return result.Length > 3 ? result.Substring(0, 3) : result;
            }
        }

        public string TeamSizeText => Discipline == null ? "" : Discipline.TeamSize + " на " + Discipline.TeamSize;

        public int TeamsCount => TournamentParticipants != null ? TournamentParticipants.Count : 0;
        public string TeamsText => TeamsCount + " / " + MaxTeams + " команд";

        public string PrizeText => PrizePool > 0 ? PrizePool.ToString("N0", Ru) + " ₽" : "—";
        public string FeeText => EntryFee > 0 ? "Взнос " + EntryFee.ToString("N0", Ru) + " ₽" : "Бесплатное участие";

        public string RegistrationText
        {
            get
            {
                DateTime now = DateTime.Now;
                if (now < RegistrationStart) return "Регистрация с " + RegistrationStart.ToString("d MMMM", Ru);
                if (now <= RegistrationEnd) return "Регистрация до " + RegistrationEnd.ToString("d MMMM, HH:mm", Ru);
                return "Регистрация закрыта";
            }
        }
    }

    public partial class User
    {
        public string FullName => string.Join(" ",
            new[] { LastName, FirstName, MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        public string ShortName => LastName + " " + Initial(FirstName) + Initial(MiddleName);

        private static string Initial(string s) =>
            string.IsNullOrWhiteSpace(s) ? "" : s.Trim()[0] + ".";

        public void SetPassword(string password)
        {
            PasswordSalt = PasswordHelper.CreateSalt();
            PasswordHash = PasswordHelper.GetHash(password, PasswordSalt);
        }
    }
}
