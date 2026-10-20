using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public static class Forms
    {
        private const string PhonePattern = @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$";

        public static IFormConfig DisciplineForm(int? id) =>
            new FormConfig<Discipline>(id, "дисциплины", "Disciplines", d => d.DisciplineID)
            .Add(new FormField
            {
                Label = "Название",
                Min = 2,
                Max = 100,
                Unique = true,
                Placeholder = "Например: Valorant",
                ToolTip = "От 2 до 100 символов, не должно повторяться"
            },
                d => d.Name, (d, v) => d.Name = (string)v)
            .Add(new FormField
            {
                Label = "Платформа",
                Kind = FieldKind.Choice,
                Min = 2,
                Max = 50,
                Default = "PC",
                Choices = new[] { "PC", "Браузер", "Консоль", "Мобильные устройства" },
                ToolTip = "Выберите из списка или введите свою (до 50 символов)"
            },
                d => d.Platform, (d, v) => d.Platform = (string)v)
            .Add(new FormField
            {
                Label = "Игроков в составе",
                Kind = FieldKind.Int,
                Min = 1,
                Max = 100,
                Default = "5",
                Placeholder = "Например: 5",
                ToolTip = "1 — индивидуальный зачёт"
            },
                d => d.TeamSize, (d, v) => d.TeamSize = (int)v)
            .Add(new FormField
            {
                Label = "Минимальный возраст",
                Kind = FieldKind.Int,
                Min = 0,
                Max = 21,
                Default = "0",
                Placeholder = "Например: 16",
                ToolTip = "Возрастное ограничение участника, лет"
            },
                d => d.AgeRating, (d, v) => d.AgeRating = (int)v)
            .Add(new FormField
            {
                Label = "Описание",
                Kind = FieldKind.MultiLine,
                Required = false,
                Max = 500,
                Placeholder = "Кратко о дисциплине",
                ToolTip = "До 500 символов"
            },
                d => d.Description, (d, v) => d.Description = (string)v);

        public static IFormConfig FormatForm(int? id) =>
            new FormConfig<TournamentFormat>(id, "формата турнира", "TournamentFormats", f => f.TournamentFormatID)
            .Add(new FormField
            {
                Label = "Название",
                Min = 2,
                Max = 100,
                Unique = true,
                Placeholder = "Например: GSL Groups",
                ToolTip = "От 2 до 100 символов"
            },
                f => f.Name, (f, v) => f.Name = (string)v)
            .Add(new FormField
            {
                Label = "Описание",
                Kind = FieldKind.MultiLine,
                Required = false,
                Max = 500,
                Placeholder = "Как проходит розыгрыш",
                ToolTip = "До 500 символов"
            },
                f => f.Description, (f, v) => f.Description = (string)v);

        public static IFormConfig StatusForm(int? id) =>
            new FormConfig<TournamentStatus>(id, "статуса турнира", "TournamentStatuses", s => s.TournamentStatusID)
            .Add(new FormField
            {
                Label = "Название",
                Min = 2,
                Max = 50,
                Unique = true,
                Placeholder = "Например: Перенесён",
                ToolTip = "От 2 до 50 символов"
            },
                s => s.Name, (s, v) => s.Name = (string)v);

        public static IFormConfig VenueForm(int? id) =>
            new FormConfig<Venue>(id, "площадки", "Venues", x => x.VenueID)
            .Add(new FormField
            {
                Label = "Название",
                Min = 2,
                Max = 100,
                Unique = true,
                Placeholder = "Например: Компьютерный класс 312",
                ToolTip = "От 2 до 100 символов"
            },
                x => x.Name, (x, v) => x.Name = (string)v)
            .Add(new FormField
            {
                Label = "Тип площадки",
                Kind = FieldKind.Choice,
                Min = 2,
                Max = 50,
                Default = "ПК-клуб",
                Choices = new[] { "ПК-клуб", "Компьютерный класс", "Арена", "Онлайн" },
                ToolTip = "Выберите из списка или введите свой (до 50 символов)"
            },
                x => x.VenueType, (x, v) => x.VenueType = (string)v)
            .Add(new FormField
            {
                Label = "Адрес",
                Required = false,
                Min = 5,
                Max = 255,
                Placeholder = "Город, улица, дом",
                ToolTip = "До 255 символов"
            },
                x => x.Address, (x, v) => x.Address = (string)v)
            .Add(new FormField
            {
                Label = "Количество мест",
                Kind = FieldKind.Int,
                Min = 0,
                Max = 10000,
                Default = "0",
                Placeholder = "Например: 40",
                ToolTip = "Сколько игровых мест; 0 — для онлайн-площадки"
            },
                x => x.SeatCount, (x, v) => x.SeatCount = (int)v)
            .Add(new FormField
            {
                Label = "Контактное лицо",
                Required = false,
                Min = 2,
                Max = 100,
                Placeholder = "Например: Орлов Сергей",
                ToolTip = "ФИО ответственного"
            },
                x => x.ContactPerson, (x, v) => x.ContactPerson = (string)v)
            .Add(new FormField
            {
                Label = "Телефон",
                Required = false,
                Min = 18,
                Max = 18,
                Placeholder = "+7 (900) 123-45-67",
                ToolTip = "Формат: +7 (900) 123-45-67",
                Mask = box => InputMask.Phone(box),
                Pattern = PhonePattern,
                PatternError = "Телефон введите в формате +7 (900) 123-45-67."
            },
                x => x.Phone, (x, v) => x.Phone = (string)v);
    }
}
