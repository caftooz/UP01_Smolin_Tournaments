using System.Globalization;
using System.Text;

namespace UP01_Smolin_Tournaments.Helpers
{
    public class FormValidator
    {
        private readonly StringBuilder _errors = new StringBuilder();
        private static readonly CultureInfo Ru = new CultureInfo("ru-RU");

        public bool HasErrors => _errors.Length > 0;

        // Произвольное условие: если false — добавить текст ошибки
        public void Require(bool condition, string message)
        {
            if (!condition) _errors.AppendLine("• " + message);
        }

        // Текст: обрезает пробелы, проверяет длину. Пустое необязательное поле -> null
        public string Text(string value, string field, int min, int max, bool required = true)
        {
            string v = (value ?? "").Trim();
            if (v.Length == 0)
            {
                if (required) _errors.AppendLine($"• Заполните поле «{field}».");
                return null;
            }
            if (v.Length < min || v.Length > max)
                _errors.AppendLine($"• Поле «{field}»: от {min} до {max} символов.");
            return v;
        }

        // Целое число в диапазоне [min; max]
        public int Int(string text, string field, int min, int max)
        {
            int value;
            if (!int.TryParse((text ?? "").Trim(), out value))
            {
                _errors.AppendLine($"• Поле «{field}»: введите целое число.");
                return 0;
            }
            if (value < min || value > max)
                _errors.AppendLine($"• Поле «{field}»: значение от {min} до {max}.");
            return value;
        }

        public decimal Money(string text, string field, decimal min, decimal max)
        {
            decimal value;
            string normalized = (text ?? "").Trim().Replace(" ", "").Replace('.', ',');
            if (!decimal.TryParse(normalized, NumberStyles.Number, Ru, out value))
            {
                _errors.AppendLine($"• Поле «{field}»: введите сумму, например 500 или 499,90.");
                return 0;
            }
            if (value < min || value > max)
                _errors.AppendLine($"• Поле «{field}»: значение от {min:N0} до {max:N0}.");
            if (decimal.Round(value, 2) != value)
                _errors.AppendLine($"• Поле «{field}»: не больше двух знаков после запятой.");
            return value;
        }

        public void Password(string password, string repeat, string mismatchMessage)
        {
            string error;
            if (!PasswordHelper.Validate(password, out error)) _errors.AppendLine("• " + error);
            Require(password == repeat, mismatchMessage);
        }

        public bool ShowErrors()
        {
            if (!HasErrors) return false;
            Msg.Error("Исправьте ошибки:\n\n" + _errors);
            return true;
        }
    }
}
