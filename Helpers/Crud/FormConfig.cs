using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public enum FieldKind
    {
        Text,      
        MultiLine, 
        Int,       
        Choice     
    }

    public class FormField
    {
        public string Label { get; set; }
        public FieldKind Kind { get; set; } = FieldKind.Text;
        public bool Required { get; set; } = true;
        public int Min { get; set; }                  
        public int Max { get; set; }                  
        public string Default { get; set; }           
        public string Placeholder { get; set; }
        public string ToolTip { get; set; }
        public string[] Choices { get; set; }         
        public Action<TextBox> Mask { get; set; }     
        public string Pattern { get; set; }           
        public string PatternError { get; set; }      
        public bool Unique { get; set; }              
    }

    public interface IFormConfig
    {
        string Title { get; }
        IList<FormField> Fields { get; }
        string[] LoadValues();                
        string CheckUnique(object[] values);  
        bool Save(object[] values);           
    }

    public class FormConfig<T> : IFormConfig where T : class, new()
    {
        private readonly int? _id;                     
        private readonly string _genitive;             
        private readonly string _tableName;            
        private readonly Func<T, int> _key;            
        private readonly List<FormField> _fields = new List<FormField>();
        private readonly List<Func<T, object>> _getters = new List<Func<T, object>>();
        private readonly List<Action<T, object>> _setters = new List<Action<T, object>>();

        public FormConfig(int? id, string genitive, string tableName, Func<T, int> key)
        {
            _id = id;
            _genitive = genitive;
            _tableName = tableName;
            _key = key;
        }

        public string Title => (_id == null ? "Добавление " : "Редактирование ") + _genitive;
        public IList<FormField> Fields => _fields;

        public FormConfig<T> Add(FormField field, Func<T, object> get, Action<T, object> set)
        {
            _fields.Add(field);
            _getters.Add(get);
            _setters.Add(set);
            return this;
        }

        public string[] LoadValues()
        {
            if (_id == null)
                return _fields.Select(f => f.Default).ToArray();

            T item = Db.Ctx.Set<T>().Find(_id.Value);
            if (item == null)
            {
                Msg.Error("Запись не найдена — возможно, она уже удалена.");
                return new string[_fields.Count];
            }
            return _getters.Select(g => Convert.ToString(g(item))).ToArray();
        }

        public string CheckUnique(object[] values)
        {
            int selfId = _id ?? 0;
            List<T> all = null; 
            for (int i = 0; i < _fields.Count; i++)
            {
                if (!_fields[i].Unique || values[i] == null) continue;
                if (all == null) all = Db.Ctx.Set<T>().ToList();

                string value = values[i].ToString();
                Func<T, object> getter = _getters[i];
                bool taken = all.Any(x => _key(x) != selfId &&
                    string.Equals(Convert.ToString(getter(x)), value, StringComparison.OrdinalIgnoreCase));
                if (taken)
                    return $"Значение «{value}» в поле «{_fields[i].Label}» уже существует.";
            }
            return null;
        }

        public bool Save(object[] values)
        {
            var db = Db.Ctx;
            var set = db.Set<T>();
            T item;
            if (_id == null)
            {
                item = new T();
                set.Add(item);
            }
            else
            {
                item = set.Find(_id.Value);
                if (item == null) { Msg.Error("Запись не найдена — возможно, её уже удалили."); return false; }
            }

            for (int i = 0; i < _fields.Count; i++)
                _setters[i](item, values[i]);

            if (!Db.TrySave()) return false;
            Audit.Write(_id == null ? "INSERT" : "UPDATE", _tableName, _key(item));
            return true;
        }
    }
}
