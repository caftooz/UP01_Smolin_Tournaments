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
        public decimal PlannedEntryRevenue => EntryFee * MaxTeams;
        public string PlaceText => IsOnline ? "Онлайн" : (Venue != null ? Venue.Name : "—");
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
