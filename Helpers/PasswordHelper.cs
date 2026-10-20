using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class PasswordHelper
    {
        public static string CreateSalt() 
        {
            byte[] salt = new byte[24];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public static string GetHash(string password, string salt)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
                return string.Concat(bytes.Select(b => b.ToString("X2")));
            }
        }

        public static bool Validate(string password, out string error)
        {
            if (password.Length < 8)
            {
                error = "Пароль слишком короткий, должно быть минимум 8 символов.";
                return false;
            }

            if (password.Any(c => c < '!' || c > '~'))
            {
                error = "Пароль содержит недопустимые символы, используйте только латинские буквы.";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                error = "Пароль должен содержать хотя бы одну цифру.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
