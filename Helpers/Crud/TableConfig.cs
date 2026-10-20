using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public class ColumnInfo
    {
        public string Header { get; set; } 
        public string Path { get; set; }    
        public string Width { get; set; }  
        public string Format { get; set; }  
        public bool IsCheckBox { get; set; }
    }

    public class FilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface ITableConfig
    {
        string Title { get; }
        string SearchHint { get; }
        string FilterHint { get; }     
        string FilterAllText { get; }    
        IList<ColumnInfo> Columns { get; }

        List<FilterOption> LoadFilter();
        IList Load(string search, int filterId);
        int IdOf(object item);
        Page CreateEditor(int? id);
        void Delete(List<int> ids);
    }

    public class TableConfig<T> : ITableConfig where T : class
    {
        public string Title { get; set; }       
        public string TableName { get; set; }     
        public string ItemName { get; set; }       
        public string SearchHint { get; set; }
        public string FilterHint { get; set; }
        public string FilterAllText { get; set; } = "Все";
        public IList<ColumnInfo> Columns { get; } = new List<ColumnInfo>();

        public Func<DB_Smolin_TournamentsEntities, IEnumerable<T>> Query { get; set; }   
        public Func<T, int> Id { get; set; }                  
        public Func<int?, Page> Editor { get; set; }               
        public Func<T, string, bool> Search { get; set; }           

        public Func<DB_Smolin_TournamentsEntities, IEnumerable<FilterOption>> FilterOptions { get; set; }  
        public Func<T, int, bool> FilterMatch { get; set; }                           
        public Func<DB_Smolin_TournamentsEntities, List<int>, string> DeleteBlocker { get; set; }         
        public Func<DB_Smolin_TournamentsEntities, List<int>, string> CascadeInfo { get; set; }      

        public TableConfig<T> Column(string header, string path, string width = "*", string format = null)
        {
            Columns.Add(new ColumnInfo { Header = header, Path = path, Width = width, Format = format });
            return this;
        }

        public TableConfig<T> CheckColumn(string header, string path)
        {
            Columns.Add(new ColumnInfo { Header = header, Path = path, Width = "Auto", IsCheckBox = true });
            return this;
        }

        public List<FilterOption> LoadFilter() =>
            FilterOptions == null ? new List<FilterOption>() : FilterOptions(Db.Ctx).ToList();

        public IList Load(string search, int filterId)
        {
            IEnumerable<T> items = Query(Db.Ctx).ToList();
            string q = (search ?? "").Trim().ToLower();
            if (q.Length > 0 && Search != null)
                items = items.Where(x => Search(x, q));
            if (filterId != 0 && FilterMatch != null)
                items = items.Where(x => FilterMatch(x, filterId));
            return items.ToList();
        }

        public int IdOf(object item) => Id((T)item);

        public Page CreateEditor(int? id) => Editor(id);

        public void Delete(List<int> ids)
        {
            var db = Db.Ctx;

            string blocker = DeleteBlocker?.Invoke(db, ids);
            if (blocker != null)
            {
                Msg.Error("Удаление невозможно: " + blocker);
                return;
            }

            string question = $"Вы действительно хотите удалить {ItemName} ({ids.Count} шт.)?";
            string cascade = CascadeInfo?.Invoke(db, ids);
            if (cascade != null) question += "\n\n" + cascade;
            if (!Msg.Confirm(question)) return;

            var set = db.Set<T>();
            var items = ids.Select(id => set.Find(id)).Where(x => x != null).ToList();
            if (items.Count == 0) { Msg.Info("Записи уже удалены."); return; }

            var deletedIds = items.Select(Id).ToList();
            set.RemoveRange(items);
            if (Db.TrySave())
            {
                foreach (int id in deletedIds) Audit.Write("DELETE", TableName, id);
                Msg.Info($"Удалено записей: {deletedIds.Count}.");
            }
        }
    }
}
