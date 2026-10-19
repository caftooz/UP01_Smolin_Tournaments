using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Ui
    {
        public static void UpdatePlaceholder(Control box, bool hasText)
        {
            if (box.Tag is UIElement placeholder)
                placeholder.Visibility = hasText ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
