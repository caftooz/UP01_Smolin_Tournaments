using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class InputMask
    {
        public static void Digits(params TextBox[] boxes) => Apply(@"^[0-9]+$", boxes);
        public static void Money(params TextBox[] boxes) => Apply(@"^[0-9.,]+$", boxes);
        public static void Time(params TextBox[] boxes) => Apply(@"^[0-9:]+$", boxes);
        public static void Phone(params TextBox[] boxes) => Apply(@"^[0-9+()\- ]+$", boxes);
        public static void Login(params TextBox[] boxes) => Apply(@"^[A-Za-z0-9_]+$", boxes);
        public static void Cyrillic(params TextBox[] boxes) => Apply(@"^[А-Яа-яЁё\-]+$", boxes);

        private static void Apply(string pattern, TextBox[] boxes)
        {
            foreach (TextBox box in boxes)
            {
                box.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, pattern);

                DataObject.AddPastingHandler(box, (s, e) =>
                {
                    string text = e.DataObject.GetData(DataFormats.UnicodeText) as string;
                    if (text == null || !Regex.IsMatch(text, pattern))
                        e.CancelCommand();
                });

                if (!Regex.IsMatch(" ", pattern))
                    box.PreviewKeyDown += (s, e) => { if (e.Key == Key.Space) e.Handled = true; };
            }
        }
    }
}
