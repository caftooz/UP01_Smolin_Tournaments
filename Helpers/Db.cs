using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Db
    {
        public static DB_Smolin_TournamentsEntities Ctx => DB_Smolin_TournamentsEntities.GetContext();

        public static bool TrySave()
        {
            try
            {
                Ctx.SaveChanges();
                return true;
            }
            catch (DbEntityValidationException ex)
            {
                var lines = ex.EntityValidationErrors
                    .SelectMany(v => v.ValidationErrors)
                    .Select(v => "• " + v.ErrorMessage);
                Fail("Данные не прошли проверку модели:\n" + string.Join("\n", lines));
            }
            catch (DbUpdateException ex)
            {
                Fail(Explain(ex));
            }
            catch (Exception ex)
            {
                Fail("Не удалось сохранить изменения: " + ex.GetBaseException().Message);
            }
            return false;
        }

        private static void Fail(string message)
        {
            DB_Smolin_TournamentsEntities.ResetContext();
            Msg.Error(message);
        }

        private static string Explain(DbUpdateException ex)
        {
            var sql = ex.GetBaseException() as SqlException;
            if (sql != null)
            {
                switch (sql.Number)
                {
                    case 547:   // FOREIGN KEY или CHECK
                        return "Операция нарушает ограничение целостности: запись связана с другими данными " +
                               "или значение вне допустимого диапазона.\n\n" + sql.Message;
                    case 2601:  // уникальный индекс
                    case 2627:  // UNIQUE / PRIMARY KEY
                        return "Такая запись уже существует (нарушена уникальность).\n\n" + sql.Message;
                }
            }
            return "Ошибка базы данных: " + ex.GetBaseException().Message;
        }
    }
}
