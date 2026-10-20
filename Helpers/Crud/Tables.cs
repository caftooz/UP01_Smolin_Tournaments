using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UP01_Smolin_Tournaments.Pages;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public static class Tables
    {
        public static ITableConfig Tournaments() => new TableConfig<Tournament>
        {
            Title = "Таблица турниров",
            TableName = "Tournaments",
            ItemName = "турниры",
            SearchHint = "Поиск по названию или организатору",
            FilterHint = "Фильтр по дисциплине",
            FilterAllText = "Все дисциплины",
            Query = db => db.Tournaments
                .Include("Discipline").Include("TournamentFormat").Include("TournamentStatus")
                .Include("Venue").Include("User")
                .OrderBy(t => t.StartDate),
            Id = t => t.TournamentID,
            Editor = id => new AddTournamentPage(id),
            Search = (t, q) => t.Name.ToLower().Contains(q) ||
                               (t.User != null && t.User.FullName.ToLower().Contains(q)),
            FilterOptions = db => db.Disciplines.OrderBy(d => d.Name)
                .Select(d => new FilterOption { Id = d.DisciplineID, Name = d.Name }),
            FilterMatch = (t, id) => t.DisciplineID == id,
            CascadeInfo = (db, ids) =>
            {
                int applications = db.TeamApplications.Count(a => ids.Contains(a.TournamentID));
                int participants = db.TournamentParticipants.Count(p => ids.Contains(p.TournamentID));
                int stages = db.Stages.Count(s => ids.Contains(s.TournamentID));
                int prizes = db.Prizes.Count(p => ids.Contains(p.TournamentID));
                int sponsors = db.TournamentSponsors.Count(s => ids.Contains(s.TournamentID));
                return "Вместе с ними будут каскадно удалены:\n" +
                       $"• заявки — {applications}\n" +
                       $"• участники — {participants}\n" +
                       $"• стадии (с группами, матчами и статистикой) — {stages}\n" +
                       $"• призовые места — {prizes}\n" +
                       $"• спонсорские договоры — {sponsors}";
            }
        }
        .Column("Название", "Name", "2*")
        .Column("Дисциплина", "Discipline.Name")
        .Column("Формат", "TournamentFormat.Name")
        .Column("Статус", "TournamentStatus.Name", "Auto")
        .Column("Начало", "StartDate", "Auto", "{0:dd.MM.yyyy HH:mm}")
        .Column("Команд", "MaxTeams", "Auto")
        .Column("Взнос, ₽", "EntryFee", "Auto", "{0:N2}")
        .Column("Призы, ₽", "PrizePool", "Auto", "{0:N0}")
        .Column("Место", "PlaceText", "Auto")
        .Column("Организатор", "User.FullName");

        public static ITableConfig Users() => new TableConfig<User>
        {
            Title = "Таблица пользователей",
            TableName = "Users",
            ItemName = "пользователей",
            SearchHint = "Поиск по логину, ФИО или e-mail",
            FilterHint = "Фильтр по роли",
            FilterAllText = "Все роли",
            Query = db => db.Users.Include("Role").ToList().OrderBy(u => u.FullName),
            Id = u => u.UserID,
            Editor = id => new AddUserPage(id),
            Search = (u, q) => u.Login.ToLower().Contains(q) || u.FullName.ToLower().Contains(q) ||
                               u.Email.ToLower().Contains(q),
            FilterOptions = db => db.Roles.OrderBy(r => r.AccessLevel)
                .Select(r => new FilterOption { Id = r.RoleID, Name = r.Name }),
            FilterMatch = (u, id) => u.RoleID == id,
            DeleteBlocker = (db, ids) =>
            {
                if (App.CurrentUser != null && ids.Contains(App.CurrentUser.UserID))
                    return "нельзя удалить собственную учётную запись.";

                int organized = db.Tournaments.Count(t => ids.Contains(t.OrganizerID));
                int players = db.Players.Count(p => ids.Contains(p.UserID));
                int referees = db.Referees.Count(r => ids.Contains(r.UserID));
                int reviewed = db.TeamApplications.Count(a => a.ReviewedBy != null && ids.Contains(a.ReviewedBy.Value));
                if (organized + players + referees + reviewed == 0) return null;

                return "у выбранных пользователей есть история —\n" +
                       $"организованные турниры ({organized}), профили игрока ({players}), " +
                       $"профили судьи ({referees}), рассмотренные заявки ({reviewed}).\n\n" +
                       "Вместо удаления откройте пользователя и снимите флажок «Активен»: " +
                       "войти он не сможет, а история сохранится.";
            },
            CascadeInfo = (db, ids) =>
                $"Каскадно удалятся их уведомления: {db.Notifications.Count(n => ids.Contains(n.UserID))}.\n" +
                "Записи журнала действий сохранятся без ссылки на пользователя."
        }
        .Column("Логин", "Login", "Auto")
        .Column("ФИО", "FullName", "2*")
        .Column("Роль", "Role.Name", "Auto")
        .Column("E-mail", "Email")
        .Column("Телефон", "Phone", "Auto")
        .CheckColumn("Активен", "IsActive")
        .Column("Регистрация", "RegisteredAt", "Auto", "{0:dd.MM.yyyy}")
        .Column("Последний вход", "LastLoginAt", "Auto", "{0:dd.MM.yyyy HH:mm}");

        public static ITableConfig Disciplines() => new TableConfig<Discipline>
        {
            Title = "Таблица дисциплин",
            TableName = "Disciplines",
            ItemName = "дисциплины",
            SearchHint = "Поиск по названию или платформе",
            Query = db => db.Disciplines.OrderBy(d => d.Name),
            Id = d => d.DisciplineID,
            Editor = id => new EditPage(Forms.DisciplineForm(id)),
            Search = (d, q) => d.Name.ToLower().Contains(q) || d.Platform.ToLower().Contains(q),
            DeleteBlocker = (db, ids) =>
            {
                int tournaments = db.Tournaments.Count(t => ids.Contains(t.DisciplineID));
                int teams = db.Teams.Count(t => ids.Contains(t.DisciplineID));
                if (tournaments + teams == 0) return null;
                return $"на выбранные дисциплины ссылаются турниры ({tournaments}) и команды ({teams}).\n" +
                       "Сначала удалите их или назначьте им другую дисциплину.";
            },
            CascadeInfo = (db, ids) =>
                $"Вместе с ними каскадно удалятся карты дисциплин: {db.Maps.Count(m => ids.Contains(m.DisciplineID))}."
        }
        .Column("Название", "Name", "2*")
        .Column("Платформа", "Platform")
        .Column("Игроков в составе", "TeamSize", "Auto")
        .Column("Возраст", "AgeRating", "Auto", "{0}+")
        .Column("Описание", "Description", "3*");

        public static ITableConfig Formats() => new TableConfig<TournamentFormat>
        {
            Title = "Таблица форматов турниров",
            TableName = "TournamentFormats",
            ItemName = "форматы",
            SearchHint = "Поиск по названию или описанию",
            Query = db => db.TournamentFormats.OrderBy(f => f.Name),
            Id = f => f.TournamentFormatID,
            Editor = id => new EditPage(Forms.FormatForm(id)),
            Search = (f, q) => f.Name.ToLower().Contains(q) ||
                               (f.Description != null && f.Description.ToLower().Contains(q)),
            DeleteBlocker = (db, ids) =>
            {
                int tournaments = db.Tournaments.Count(t => ids.Contains(t.TournamentFormatID));
                return tournaments == 0 ? null : $"выбранные форматы используются в турнирах ({tournaments}).";
            }
        }
        .Column("Название", "Name")
        .Column("Описание", "Description", "2*");

        public static ITableConfig Statuses() => new TableConfig<TournamentStatus>
        {
            Title = "Таблица статусов турниров",
            TableName = "TournamentStatuses",
            ItemName = "статусы",
            SearchHint = "Поиск по названию",
            Query = db => db.TournamentStatuses.Include("Tournaments").OrderBy(s => s.TournamentStatusID),
            Id = s => s.TournamentStatusID,
            Editor = id => new EditPage(Forms.StatusForm(id)),
            Search = (s, q) => s.Name.ToLower().Contains(q),
            DeleteBlocker = (db, ids) =>
            {
                int tournaments = db.Tournaments.Count(t => ids.Contains(t.TournamentStatusID));
                return tournaments == 0 ? null : $"выбранные статусы назначены турнирам ({tournaments}).";
            }
        }
        .Column("Название", "Name")
        .Column("Турниров", "Tournaments.Count", "Auto");

        public static ITableConfig Venues() => new TableConfig<Venue>
        {
            Title = "Таблица площадок",
            TableName = "Venues",
            ItemName = "площадки",
            SearchHint = "Поиск по названию, типу или адресу",
            Query = db => db.Venues.OrderBy(v => v.Name),
            Id = v => v.VenueID,
            Editor = id => new EditPage(Forms.VenueForm(id)),
            Search = (v, q) => v.Name.ToLower().Contains(q) || v.VenueType.ToLower().Contains(q) ||
                               (v.Address != null && v.Address.ToLower().Contains(q)),
            DeleteBlocker = (db, ids) =>
            {
                int tournaments = db.Tournaments.Count(t => t.VenueID != null && ids.Contains(t.VenueID.Value));
                return tournaments == 0 ? null : $"на выбранных площадках проводятся турниры ({tournaments}).";
            }
        }
        .Column("Название", "Name")
        .Column("Тип", "VenueType", "Auto")
        .Column("Адрес", "Address", "2*")
        .Column("Мест", "SeatCount", "Auto")
        .Column("Контакт", "ContactPerson")
        .Column("Телефон", "Phone", "Auto");
    }
}
