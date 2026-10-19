using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UP01_Smolin_Tournaments.Properties;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class ThemeManager
    {
        public const string Light = "Dictionary.xaml";      
        public const string Dark = "CustomDictionary.xaml"; 

        public static void Apply(string name)
        {
            var dict = (ResourceDictionary)System.Windows.Application.LoadComponent(new Uri(name, UriKind.Relative));
            System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

            Settings.Default.Theme = name;
            Settings.Default.Save();
        }

        public static void Load()
        {
            Apply(Settings.Default.Theme == Dark ? Dark : Light);
        }

        public static void Toggle()
        {
            Apply(Settings.Default.Theme == Dark ? Light : Dark);
        }
    }
}
