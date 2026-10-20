using System;
using System.Collections.Generic;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public class FieldUi
    {
        public string Label, Placeholder, Hint, Pattern, PatternError;
        public decimal Min, Max = int.MaxValue;
        public int MinLength = 1;
        public MaskKind Mask;
    }

    public static class UiMetadata
    {
        private const string NamePattern = @"^[А-ЯЁа-яё]+(-[А-ЯЁа-яё]+)?$";

        private static readonly Dictionary<string, FieldUi> Fields = new Dictionary<string, FieldUi>
        {
            { "Name", new FieldUi { Label = "Название", MinLength = 2 } },
            { "Tournament.Name", new FieldUi { Label = "Название", MinLength = 3, Placeholder = "Например: Кубок клуба по Dota 2" } },
            { "Discipline.Name", new FieldUi { Label = "Название", MinLength = 2, Placeholder = "Например: Valorant" } },
            { "Venue.Name", new FieldUi { Label = "Название", MinLength = 2, Placeholder = "Например: Компьютерный класс 312" } },
            { "Description", new FieldUi { Label = "Описание" } },
            { "Platform", new FieldUi { Label = "Платформа", MinLength = 2, Placeholder = "PC, Браузер, Консоль…" } },
            { "TeamSize", new FieldUi { Label = "Игроков в составе", Min = 1, Max = 100, Hint = "1 — индивидуальный зачёт" } },
            { "AgeRating", new FieldUi { Label = "Минимальный возраст", Min = 0, Max = 21 } },
            { "DisciplineID", new FieldUi { Label = "Дисциплина" } },
            { "TournamentFormatID", new FieldUi { Label = "Формат розыгрыша" } },
            { "TournamentStatusID", new FieldUi { Label = "Статус" } },
            { "OrganizerID", new FieldUi { Label = "Организатор", Hint = "Активный пользователь с ролью «Организатор» или «Администратор»" } },
            { "VenueID", new FieldUi { Label = "Площадка", Hint = "Обязательна для офлайн-турнира" } },
            { "IsOnline", new FieldUi { Label = "Онлайн", Hint = "Отметьте, если турнир проводится через интернет (площадка не нужна)" } },
            { "RegistrationStart", new FieldUi { Label = "Начало регистрации" } },
            { "RegistrationEnd", new FieldUi { Label = "Окончание регистрации" } },
            { "StartDate", new FieldUi { Label = "Дата и время начала" } },
            { "EndDate", new FieldUi { Label = "Дата и время окончания" } },
            { "MaxTeams", new FieldUi { Label = "Максимум команд", Min = 2, Max = 128 } },
            { "EntryFee", new FieldUi { Label = "Взнос, ₽", Min = 0, Max = 100000, Placeholder = "Например: 500" } },
            { "PrizePool", new FieldUi { Label = "Призовой фонд, ₽", Min = 0, Max = 10000000, Placeholder = "Например: 20000" } },
            { "Rules", new FieldUi { Label = "Регламент", Placeholder = "Например: серии BO3, финал BO5" } },
            { "Login", new FieldUi { Label = "Логин", MinLength = 3, Placeholder = "Например: org_sidorov", Mask = MaskKind.Login,
                                     Pattern = @"^[A-Za-z0-9_]+$", PatternError = "Логин: только латинские буквы, цифры и «_»." } },
            { "LastName", new FieldUi { Label = "Фамилия", MinLength = 2, Placeholder = "Сидоров", Mask = MaskKind.Cyrillic,
                                        Pattern = NamePattern, PatternError = "Фамилия: только русские буквы и дефис." } },
            { "FirstName", new FieldUi { Label = "Имя", MinLength = 2, Placeholder = "Олег", Mask = MaskKind.Cyrillic,
                                         Pattern = NamePattern, PatternError = "Имя: только русские буквы и дефис." } },
            { "MiddleName", new FieldUi { Label = "Отчество", MinLength = 2, Placeholder = "Игоревич", Mask = MaskKind.Cyrillic,
                                          Pattern = NamePattern, PatternError = "Отчество: только русские буквы и дефис." } },
            { "BirthDate", new FieldUi { Label = "Дата рождения", Hint = "Возраст от 10 до 100 лет" } },
            { "Email", new FieldUi { Label = "E-mail", MinLength = 6, Placeholder = "name@mail.ru",
                                     Pattern = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", PatternError = "E-mail введите в формате name@mail.ru." } },
            { "Phone", new FieldUi { Label = "Телефон", MinLength = 18, Placeholder = "+7 (900) 123-45-67", Mask = MaskKind.Phone,
                                     Pattern = @"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$", PatternError = "Телефон введите в формате +7 (900) 123-45-67." } },
            { "RoleID", new FieldUi { Label = "Роль" } },
            { "RegisteredAt", new FieldUi { Label = "Регистрация" } },
            { "LastLoginAt", new FieldUi { Label = "Последний вход" } },
            { "IsActive", new FieldUi { Label = "Активен", Hint = "Снимите флажок, чтобы заблокировать вход без удаления истории" } },
            { "VenueType", new FieldUi { Label = "Тип площадки", MinLength = 2, Placeholder = "ПК-клуб, Арена…" } },
            { "Address", new FieldUi { Label = "Адрес", MinLength = 5, Placeholder = "Город, улица, дом" } },
            { "SeatCount", new FieldUi { Label = "Количество мест", Min = 0, Max = 10000, Hint = "0 — для онлайн-площадки" } },
            { "ContactPerson", new FieldUi { Label = "Контактное лицо", MinLength = 2, Placeholder = "Например: Орлов Сергей" } },
            { "DefaultSanction", new FieldUi { Label = "Санкция по умолчанию" } },
            { "PenaltyPoints", new FieldUi { Label = "Штрафные очки", Min = 0, Max = 100 } }
        };

        private static readonly Dictionary<string, string> Plurals = new Dictionary<string, string>
        {
            { "Tournaments", "турниры" }, { "Users", "пользователи" }, { "Disciplines", "дисциплины" },
            { "TournamentFormats", "форматы турниров" }, { "TournamentStatuses", "статусы турниров" }, { "Venues", "площадки" },
            { "ViolationTypes", "типы нарушений" }, { "MatchStatuses", "статусы матчей" }, { "Applications", "заявки" },
            { "Maps", "карты" }, { "Notifications", "уведомления" }, { "Prizes", "призовые места" },
            { "Stages", "стадии" }, { "TournamentParticipants", "участники" }, { "TournamentSponsors", "спонсорские договоры" }
        };

        private static readonly Dictionary<string, string> Genitives = new Dictionary<string, string>
        {
            { "Tournaments", "турнира" }, { "Users", "пользователя" }, { "Disciplines", "дисциплины" },
            { "TournamentFormats", "формата турнира" }, { "TournamentStatuses", "статуса турнира" }, { "Venues", "площадки" },
            { "ViolationTypes", "типа нарушения" }, { "MatchStatuses", "статуса матча" }
        };

        private static readonly HashSet<string> HiddenInForm = new HashSet<string>
            { "PasswordHash", "PasswordSalt", "CreatedAt", "RegisteredAt", "LastLoginAt", "LogoPath" };

        private static readonly HashSet<string> HiddenInTable = new HashSet<string>
            { "PasswordHash", "PasswordSalt", "LogoPath", "Tournament.RegistrationStart", "Tournament.RegistrationEnd", "Tournament.CreatedAt" };

        public static FieldUi Field(Type type, string property)
        {
            FieldUi field;
            if (Fields.TryGetValue(type.Name + "." + property, out field) || Fields.TryGetValue(property, out field)) return field;
            return new FieldUi { Label = property };
        }

        public static string Plural(string table) => Plurals.ContainsKey(table) ? Plurals[table] : table;
        public static string Genitive(string table) => Genitives.ContainsKey(table) ? Genitives[table] : table;
        public static string DisplayMember(Type type) => type == typeof(User) ? "FullName" : "Name";

        public static bool IsHiddenInForm(PropertyMeta p) => p.IsIdentity || HiddenInForm.Contains(p.Name);

        public static bool IsHiddenInTable(Type type, PropertyMeta p) =>
            p.IsKey || (p.Type == typeof(string) && (p.MaxLength ?? int.MaxValue) > 500) ||
            HiddenInTable.Contains(p.Name) || HiddenInTable.Contains(type.Name + "." + p.Name);
    }
}
