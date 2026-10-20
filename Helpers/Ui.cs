using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Ui
    {
        public static void UpdatePlaceholder(Control box, bool hasText)
        {
            if (box.Tag is UIElement placeholder)
                placeholder.Visibility = hasText ? Visibility.Hidden : Visibility.Visible;
        }

        public static void RegisterPlaceholders()
        {
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.TextChangedEvent,
                new TextChangedEventHandler((s, e) => Refresh((TextBox)s)));
            EventManager.RegisterClassHandler(typeof(PasswordBox), PasswordBox.PasswordChangedEvent,
                new RoutedEventHandler((s, e) => Refresh((PasswordBox)s)));

            EventManager.RegisterClassHandler(typeof(TextBox), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) => RefreshLater((Control)s)));
            EventManager.RegisterClassHandler(typeof(PasswordBox), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((s, e) => RefreshLater((Control)s)));
        }

        private static void Refresh(Control box)
        {
            var text = box as TextBox;
            var pass = box as PasswordBox;
            bool hasText = text != null ? text.Text.Length > 0 : pass != null && pass.Password.Length > 0;
            UpdatePlaceholder(box, hasText);
        }

        private static void RefreshLater(Control box) =>
            box.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new System.Action(() => Refresh(box)));
    }
}
