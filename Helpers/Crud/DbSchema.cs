using System.Collections.Generic;
using System.Linq;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public class ForeignKeyRule
    {
        public string Parent { get; set; }
        public string Child { get; set; }
        public string ColumnName { get; set; }
        public int Action { get; set; }
    }

    public static class DbSchema
    {
        private const int Cascade = 1;
        private static List<ForeignKeyRule> _rules;

        private static List<ForeignKeyRule> Rules => _rules ?? (_rules = Db.Ctx.Database.SqlQuery<ForeignKeyRule>(
            @"SELECT OBJECT_NAME(fk.referenced_object_id) AS Parent, OBJECT_NAME(fk.parent_object_id) AS Child,
                     COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
                     CAST(fk.delete_referential_action AS int) AS Action
              FROM sys.foreign_keys fk
              JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id").ToList());

        public static string CascadeInfo(string table, List<int> ids)
        {
            List<string> parts = Rules.Where(r => r.Parent == table && r.Child != table && r.Action == Cascade)
                .GroupBy(r => r.Child)
                .Select(g => new
                {
                    Table = g.Key,
                    Count = Db.Ctx.Database.SqlQuery<int>($"SELECT COUNT(*) FROM [{g.Key}] WHERE " +
                        string.Join(" OR ", g.Select(r => $"[{r.ColumnName}] IN ({string.Join(",", ids)})"))).Single()
                })
                .Where(c => c.Count > 0)
                .Select(c => $"{UiMetadata.Plural(c.Table)} ({c.Count})")
                .ToList();
            return parts.Count == 0 ? null : "Вместе с ними будут каскадно удалены (включая вложенные записи): " + string.Join(", ", parts) + ".";
        }
    }
}
