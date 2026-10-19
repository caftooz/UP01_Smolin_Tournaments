using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
