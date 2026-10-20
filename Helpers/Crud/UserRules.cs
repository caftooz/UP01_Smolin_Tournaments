using System;
using System.Collections.Generic;
using System.Linq;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public static class UserRules
    {
        public static EntityRules Create()
        {
            var rules = new EntityRules
            {
                OnCreate = entity => SetDefaults((User)entity),
                Validate = (entity, v, ctx) => Validate((User)entity, v, ctx),
                BeforeSave = (entity, ctx) =>
                {
                    if (PasswordEntered(ctx)) ((User)entity).SetPassword(ctx.Password("Password"));
                },
                AfterSave = (entity, ctx) =>
                {
                    if (IsMe(ctx.Id)) App.CurrentUser = Db.Ctx.Users.Include("Role").AsNoTracking().First(u => u.UserID == ctx.Id);
                },
                CanDelete = CanDelete
            };
            rules.VirtualFields.Add(new VirtualField("Password", "Пароль", "Новый пароль (оставьте пустым, чтобы не менять)",
                                                     "От 8 символов, латиница, хотя бы одна цифра"));
            rules.VirtualFields.Add(new VirtualField("PasswordRepeat", "Повтор пароля", "Повтор нового пароля", "Повторите пароль"));
            rules.Choices["RoleID"] = () => Db.Ctx.Roles.OrderBy(r => r.AccessLevel).ToList();
            return rules;
        }

        private static bool IsMe(int id) => App.CurrentUser != null && App.CurrentUser.UserID == id;

        private static bool PasswordEntered(EditContext ctx) =>
            ctx.IsNew || ctx.Password("Password") != "" || ctx.Password("PasswordRepeat") != "";

        private static void SetDefaults(User u)
        {
            u.RegisteredAt = DateTime.Now;
            u.IsActive = true;
            u.RoleID = Db.Ctx.Roles.Where(r => r.Name == Session.PlayerRole).Select(r => r.RoleID).FirstOrDefault();
        }

        private static void Validate(User u, FormValidator v, EditContext ctx)
        {
            int roleId = u.RoleID;
            if (IsMe(ctx.Id))
            {
                v.Require(u.IsActive, "Нельзя заблокировать собственную учётную запись.");
                v.Require(Db.Ctx.Roles.Any(r => r.RoleID == roleId && r.Name == Session.AdminRole), "Нельзя снять с себя роль администратора.");
            }
            DateTime birth = u.BirthDate;
            v.Require(birth == default(DateTime) || (birth <= DateTime.Today.AddYears(-10) && birth >= DateTime.Today.AddYears(-100)),
                      "Возраст пользователя должен быть от 10 до 100 лет.");
            if (PasswordEntered(ctx))
                v.Password(ctx.Password("Password"), ctx.Password("PasswordRepeat"), "Пароли не совпадают.");
        }

        private static string CanDelete(List<int> ids)
        {
            if (App.CurrentUser != null && ids.Contains(App.CurrentUser.UserID))
                return "нельзя удалить собственную учётную запись.";

            var db = Db.Ctx;
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
        }
    }
}
