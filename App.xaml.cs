using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using UP01_Smolin_Tournaments.Helpers;

namespace UP01_Smolin_Tournaments
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static User CurrentUser { get; set; }

        public App()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU")));

            Ui.RegisterPlaceholders();

            DispatcherUnhandledException += (s, e) =>
            {
                DB_Smolin_TournamentsEntities.ResetContext();
                Msg.Error("Произошла непредвиденная ошибка:\n" + e.Exception.GetBaseException().Message);
                e.Handled = true;
            };
        }
    }
}
