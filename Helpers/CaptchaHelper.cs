using System;
using System.Text;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class CaptchaHelper
    {
        private static readonly Random Rnd = new Random();
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        public static string Generate(int length = 6)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
                sb.Append(Chars[Rnd.Next(Chars.Length)]);
            return sb.ToString();
        }
    }
}
