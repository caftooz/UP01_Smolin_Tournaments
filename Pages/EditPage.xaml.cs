using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using UP01_Smolin_Tournaments.Helpers;
using UP01_Smolin_Tournaments.Helpers.Crud;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для EditPage.xaml
    /// </summary>
    public partial class EditPage : Page
    {
        private const int MaxTextLength = 4000;

        private class FormField
        {
            public PropertyMeta Meta;
            public FieldUi Ui;
            public Control Input;
            public TextBox Time;
        }

        private readonly EntityInfo _info;
        private readonly EntityRules _rules;
        private readonly EditContext _ctx;
        private readonly object _entity;
        private readonly List<FormField> _fields = new List<FormField>();

        public EditPage(Type type, int? id)
        {
            InitializeComponent();
            _info = EntityInfo.Of(type);
            _rules = EntityRules.For(type);
            _ctx = new EditContext { IsNew = id == null, Id = id ?? 0 };
            Title = (id == null ? "Добавление " : "Редактирование ") + UiMetadata.Genitive(_info.TableName);
            TbHeader.Text = Title;

            _entity = id == null ? Activator.CreateInstance(type) : Db.Ctx.Set(type).Find(id.Value);
            if (_entity == null) { Msg.Error("Запись не найдена — возможно, она уже удалена."); return; }
            if (id == null) _rules.OnCreate(_entity);

            foreach (PropertyMeta p in _info.Properties.Where(p => !UiMetadata.IsHiddenInForm(p)))
                AddField(p);
            foreach (VirtualField f in _rules.VirtualFields)
            {
                AddLabel(!_ctx.IsNew ? f.EditLabel : f.Label + " *");
                var box = new PasswordBox { MaxLength = 50, ToolTip = f.ToolTip };
                Field.SetHint(box, f.Label);
                FieldsPanel.Children.Add(box);
                _ctx.Controls[f.Name] = box;
            }
            _rules.FormReady(_ctx);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Session.DenyIfNotAdmin(this);

        private void AddField(PropertyMeta p)
        {
            var f = new FormField { Meta = p, Ui = UiMetadata.Field(_info.Type, p.Name) };
            object value = p.GetValue(_entity);
            if (p.Type == typeof(bool))
            {
                f.Input = new CheckBox { Content = f.Ui.Label, IsChecked = Equals(value, true), Margin = new Thickness(5, 10, 5, 0) };
                FieldsPanel.Children.Add(f.Input);
            }
            else
            {
                AddLabel(f.Ui.Label + (p.IsNullable ? "" : " *") + (p.IsNumber ? $" (от {f.Ui.Min:#,0} до {f.Ui.Max:#,0})" : ""));
                if (p.IsForeignKey) f.Input = AddComboBox(p, value);
                else if (p.Type == typeof(DateTime)) f.Input = AddDatePicker(f, value as DateTime?);
                else f.Input = AddTextBox(f, value);
            }
            f.Input.ToolTip = f.Ui.Hint ?? Describe(f);
            _fields.Add(f);
            _ctx.Controls[p.Name] = f.Input;
        }

        private void AddLabel(string text)
        {
            var label = new TextBlock { Text = text };
            label.SetResourceReference(StyleProperty, "FieldLabel");
            FieldsPanel.Children.Add(label);
        }

        private static string Describe(FormField f)
        {
            PropertyMeta p = f.Meta;
            string rule = p.IsForeignKey ? "выберите значение из списка"
                : p.IsNumber ? $"число от {f.Ui.Min:#,0} до {f.Ui.Max:#,0}"
                : p.Type == typeof(string) ? $"от {f.Ui.MinLength} до {p.MaxLength ?? MaxTextLength} символов. {f.Ui.PatternError}"
                : "дата";
            return p.Type == typeof(bool) ? "Отметьте или снимите флажок" : (p.IsNullable ? "Необязательно: " : "Обязательно: ") + rule;
        }

        private ComboBox AddComboBox(PropertyMeta p, object value)
        {
            List<object> items = (_rules.Choices.ContainsKey(p.Name) ? _rules.Choices[p.Name]() : p.Related.LoadAll(false)).ToList();
            if (p.IsNullable) items.Insert(0, null);
            var combo = new ComboBox { ItemsSource = items, DisplayMemberPath = UiMetadata.DisplayMember(p.RelatedType) };
            combo.SelectedValuePath = p.Related.Key.Name;
            combo.SelectedValue = value;
            FieldsPanel.Children.Add(combo);
            return combo;
        }

        private DatePicker AddDatePicker(FormField f, DateTime? date)
        {
            if (date == default(DateTime)) date = null;
            var picker = new DatePicker { SelectedDate = date?.Date };
            if (!f.Meta.HasTime)
            {
                FieldsPanel.Children.Add(picker);
                return picker;
            }

            f.Time = new TextBox { Width = 120, MaxLength = 5, Text = date?.ToString("HH:mm") ?? "", ToolTip = "Время в формате чч:мм, например 18:30" };
            Field.SetHint(f.Time, "чч:мм");
            Field.SetMask(f.Time, MaskKind.Time);
            DockPanel.SetDock(f.Time, Dock.Right);
            FieldsPanel.Children.Add(new DockPanel { Children = { f.Time, picker } });
            return picker;
        }

        private TextBox AddTextBox(FormField f, object value)
        {
            PropertyMeta p = f.Meta;
            var box = new TextBox();
            box.MaxLength = p.Type == typeof(string) ? p.MaxLength ?? MaxTextLength
                : f.Ui.Max.ToString("0").Length + (p.Type == typeof(decimal) ? 3 : 0);
            Field.SetMask(box, p.Type == typeof(int) ? MaskKind.Digits : p.Type == typeof(decimal) ? MaskKind.Money : f.Ui.Mask);
            Field.SetHint(box, f.Ui.Placeholder ?? f.Ui.Label + (p.IsNullable ? " — необязательно" : ""));
            if (p.IsMultiLine)
            {
                box.Height = 70;
                box.TextWrapping = TextWrapping.Wrap;
                box.AcceptsReturn = true;
                box.VerticalContentAlignment = VerticalAlignment.Top;
            }

            box.Text = value is decimal ? ((decimal)value).ToString("0.##") : Convert.ToString(value);
            FieldsPanel.Children.Add(box);
            return box;
        }

        private static object Read(FormField f, FormValidator v)
        {
            PropertyMeta p = f.Meta;
            string label = f.Ui.Label;

            var check = f.Input as CheckBox;
            if (check != null) return check.IsChecked == true;

            var combo = f.Input as ComboBox;
            if (combo != null)
            {
                v.Require(combo.SelectedValue != null || p.IsNullable, $"Выберите значение в поле «{label}».");
                return combo.SelectedValue;
            }

            var picker = f.Input as DatePicker;
            if (picker != null)
            {
                if (picker.SelectedDate == null)
                {
                    v.Require(p.IsNullable, $"Укажите дату в поле «{label}».");
                    return null;
                }
                TimeSpan time = TimeSpan.Zero;
                if (f.Time != null && f.Time.Text.Trim() != "")
                    v.Require(TimeSpan.TryParseExact(f.Time.Text.Trim(), new[] { @"hh\:mm", @"h\:mm" }, CultureInfo.InvariantCulture, out time),
                              $"«{label}»: время укажите в формате чч:мм, например 18:30.");
                return picker.SelectedDate.Value.Date + time;
            }

            string text = ((TextBox)f.Input).Text;
            if (p.Type == typeof(string))
            {
                string s = v.Text(text, label, f.Ui.MinLength, p.MaxLength ?? MaxTextLength, !p.IsNullable);
                if (s != null && f.Ui.Pattern != null) v.Require(Regex.IsMatch(s, f.Ui.Pattern), f.Ui.PatternError);
                return s;
            }
            if (p.IsNullable && text.Trim() == "") return null;
            if (p.Type == typeof(int)) return v.Int(text, label, (int)f.Ui.Min, (int)f.Ui.Max);
            return v.Money(text, label, f.Ui.Min, f.Ui.Max);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_entity == null) return;

            var v = new FormValidator();
            object draft = _info.CopyOf(_entity);
            foreach (FormField f in _fields)
            {
                object value = Read(f, v);
                if (value != null || f.Meta.IsNullable) f.Meta.SetValue(draft, value);
            }

            _rules.Validate(draft, v, _ctx);
            if (v.ShowErrors()) return;

            DbSet set = Db.Ctx.Set(_info.Type);
            object target = _ctx.IsNew ? set.Add(draft) : set.Find(_ctx.Id);
            if (target == null) { Msg.Error("Запись не найдена — возможно, её уже удалили."); return; }
            foreach (FormField f in _fields)
                f.Meta.SetValue(target, f.Meta.GetValue(draft));

            _rules.BeforeSave(target, _ctx);
            if (!Db.TrySave()) return;

            Audit.Write(_ctx.IsNew ? "INSERT" : "UPDATE", _info.TableName, _info.KeyOf(target));
            _rules.AfterSave(target, _ctx);
            Msg.Info("Запись сохранена.");
            NavigationService?.GoBack();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}
