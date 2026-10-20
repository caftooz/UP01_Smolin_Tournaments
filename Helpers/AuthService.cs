using System;
using System.Linq;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class AuthService
    {
        public static User Authenticate(string login, string password)
        {
            var user = DB_Smolin_TournamentsEntities.GetContext().Users
                .Include("Role")
                .AsNoTracking()
                .FirstOrDefault(u => u.Login == login && u.IsActive);
            if (user == null) return null;
            string hash = PasswordHelper.GetHash(password, user.PasswordSalt);
            return hash == user.PasswordHash ? user : null;
        }

        public static void RegisterLogin(int userId)
        {
            User tracked = DB_Smolin_TournamentsEntities.GetContext().Users.Find(userId);
            if (tracked == null) return;
            tracked.LastLoginAt = DateTime.Now;
            Db.TrySave();
            Audit.Write("LOGIN", "Users", userId, userId);
        }
    }
}
