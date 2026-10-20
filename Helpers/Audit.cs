using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP01_Smolin_Tournaments.Helpers
{
    public static class Audit
    {
        // action: LOGIN / PASSWORD / INSERT / UPDATE / DELETE / EXPORT
        public static void Write(string action, string tableName = null, int? recordId = null, int? userId = null)
        {
            try
            {
                using (var db = new DB_Smolin_TournamentsEntities())
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        UserID = userId ?? App.CurrentUser?.UserID,
                        Action = action,
                        TableName = tableName,
                        RecordID = recordId,
                        OccurredAt = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch
            {
            }
        }
    }
}
