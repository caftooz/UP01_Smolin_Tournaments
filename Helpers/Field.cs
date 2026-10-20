using System.Windows;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers
{
    public enum MaskKind { None, Digits, Money, Time, Phone, Login, Cyrillic, FullName }

    public static class Field
    {
        public static readonly DependencyProperty HintProperty = DependencyProperty.RegisterAttached(
            "Hint", typeof(string), typeof(Field), new PropertyMetadata(null, OnHintChanged));

        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.RegisterAttached(
            "IsEmpty", typeof(bool), typeof(Field), new PropertyMetadata(true));

        public static readonly DependencyProperty MaskProperty = DependencyProperty.RegisterAttached(
            "Mask", typeof(MaskKind), typeof(Field), new PropertyMetadata(MaskKind.None, OnMaskChanged));

        public static string GetHint(DependencyObject element) => (string)element.GetValue(HintProperty);
        public static void SetHint(DependencyObject element, string value) => element.SetValue(HintProperty, value);

        public static bool GetIsEmpty(DependencyObject element) => (bool)element.GetValue(IsEmptyProperty);
        public static void SetIsEmpty(DependencyObject element, bool value) => element.SetValue(IsEmptyProperty, value);

        public static MaskKind GetMask(DependencyObject element) => (MaskKind)element.GetValue(MaskProperty);
        public static void SetMask(DependencyObject element, MaskKind value) => element.SetValue(MaskProperty, value);

        private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;
            if (box != null && e.OldValue == null)
                box.PasswordChanged += (s, args) => SetIsEmpty(box, box.Password.Length == 0);
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as TextBox;
            if (box == null) return;
            switch ((MaskKind)e.NewValue)
            {
                case MaskKind.Digits: InputMask.Digits(box); break;
                case MaskKind.Money: InputMask.Money(box); break;
                case MaskKind.Time: InputMask.Time(box); break;
                case MaskKind.Phone: InputMask.Phone(box); break;
                case MaskKind.Login: InputMask.Login(box); break;
                case MaskKind.Cyrillic: InputMask.Cyrillic(box); break;
                case MaskKind.FullName: InputMask.FullName(box); break;
            }
        }
    }
}
