using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Msg
    {
        public static void Info(string text) =>
            MessageBox.Show(text, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Error(string text) =>
            MessageBox.Show(text, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

        public static bool Confirm(string text) =>
            MessageBox.Show(text, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
