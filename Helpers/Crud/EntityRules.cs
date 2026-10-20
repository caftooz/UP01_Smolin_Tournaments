using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public class VirtualField
    {
        public VirtualField(string name, string label, string editLabel, string toolTip)
        {
            Name = name;
            Label = label;
            EditLabel = editLabel;
            ToolTip = toolTip;
        }

        public string Name { get; }
        public string Label { get; }
        public string EditLabel { get; }
        public string ToolTip { get; }
    }

    public class EditContext
    {
        public bool IsNew { get; set; }
        public int Id { get; set; }
        public Dictionary<string, Control> Controls { get; } = new Dictionary<string, Control>();

        public string Password(string name) => ((PasswordBox)Controls[name]).Password;
    }

    public class EntityRules
    {
        private static readonly Dictionary<Type, EntityRules> Registry = new Dictionary<Type, EntityRules>
        {
            { typeof(Tournament), TournamentRules.Create() },
            { typeof(User), UserRules.Create() }
        };

        public Action<object> OnCreate { get; set; } = entity => { };
        public Action<object, FormValidator, EditContext> Validate { get; set; } = (entity, v, ctx) => { };
        public Action<object, EditContext> BeforeSave { get; set; } = (entity, ctx) => { };
        public Action<object, EditContext> AfterSave { get; set; } = (entity, ctx) => { };
        public Action<EditContext> FormReady { get; set; } = ctx => { };
        public Func<List<int>, string> CanDelete { get; set; } = ids => null;
        public List<VirtualField> VirtualFields { get; } = new List<VirtualField>();
        public Dictionary<string, Func<IEnumerable<object>>> Choices { get; } = new Dictionary<string, Func<IEnumerable<object>>>();

        public static EntityRules For(Type type)
        {
            EntityRules rules;
            return Registry.TryGetValue(type, out rules) ? rules : new EntityRules();
        }
    }
}
