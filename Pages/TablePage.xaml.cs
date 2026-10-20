using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using UP01_Smolin_Tournaments.Helpers;
using UP01_Smolin_Tournaments.Helpers.Crud;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для TablePage.xaml
    /// </summary>
    public partial class TablePage : Page
    {
        private readonly EntityInfo _info;
        private readonly List<Func<object, string>> _searchFields = new List<Func<object, string>>();
        private List<object> _all = new List<object>();

        public TablePage(Type type)
        {
            InitializeComponent();
            _info = EntityInfo.Of(type);
            Title = "Таблица: " + UiMetadata.Plural(_info.TableName);

            foreach (PropertyMeta p in _info.Properties.Where(p => !UiMetadata.IsHiddenInTable(type, p)))
            {
                GridItems.Columns.Add(CreateColumn(p, UiMetadata.Field(type, p.Name).Label));
                if (p.IsForeignKey) _searchFields.Add(x => p.Related.DisplayOf(p.Navigation.GetValue(x)));
                else if (p.Type == typeof(string)) _searchFields.Add(x => (string)p.GetValue(x));
            }
        }

        private static DataGridColumn CreateColumn(PropertyMeta p, string header)
        {
            var binding = new Binding(p.IsForeignKey ? p.Navigation.Name + "." + UiMetadata.DisplayMember(p.RelatedType) : p.Name);
            if (p.Type == typeof(DateTime)) binding.StringFormat = p.HasTime ? "{0:dd.MM.yyyy HH:mm}" : "{0:dd.MM.yyyy}";
            if (p.Type == typeof(decimal)) binding.StringFormat = "{0:N2}";
            if (p.Type == typeof(bool)) return new DataGridCheckBoxColumn { Header = header, Binding = binding };

            bool wide = p.Type == typeof(string) || p.IsForeignKey;
            return new DataGridTextColumn
            {
                Header = header,
                Binding = binding,
                MinWidth = wide ? 110 : 20,
                Width = wide ? new DataGridLength(1, DataGridLengthUnitType.Star) : DataGridLength.Auto
            };
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || Session.DenyIfNotAdmin(this)) return;
            _all = _info.LoadAll(true);
            ApplySearch();
        }

        private void ApplySearch()
        {
            string q = TbSearch.Text.Trim().ToLower();
            List<object> items = _all.Where(x => q == "" || _searchFields.Any(f => (f(x) ?? "").ToLower().Contains(q))).ToList();
            GridItems.ItemsSource = items;
            TbCount.Text = $"Записей: {items.Count} из {_all.Count}";
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void BtnAdd_Click(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new EditPage(_info.Type, null));

        private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

        private void GridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(GridItems, e.OriginalSource as DependencyObject) is DataGridRow)
                EditSelected();
        }

        private void EditSelected()
        {
            if (GridItems.SelectedItem == null) { Msg.Info("Выделите запись в таблице."); return; }
            NavigationService?.Navigate(new EditPage(_info.Type, _info.KeyOf(GridItems.SelectedItem)));
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            List<int> ids = GridItems.SelectedItems.Cast<object>().Select(_info.KeyOf).ToList();
            if (ids.Count == 0) { Msg.Info("Выделите записи, которые нужно удалить."); return; }

            string blocker = EntityRules.For(_info.Type).CanDelete(ids);
            if (blocker != null) { Msg.Error("Удаление невозможно: " + blocker); return; }

            string question = $"Вы действительно хотите удалить {UiMetadata.Plural(_info.TableName)} ({ids.Count} шт.)?";
            string cascade = DbSchema.CascadeInfo(_info.TableName, ids);
            if (!Msg.Confirm(cascade == null ? question : question + "\n\n" + cascade)) return;

            DbSet set = Db.Ctx.Set(_info.Type);
            set.RemoveRange(ids.Select(id => set.Find(id)).Where(x => x != null).ToList());
            if (Db.TrySave())
            {
                foreach (int id in ids) Audit.Write("DELETE", _info.TableName, id);
                Msg.Info($"Удалено записей: {ids.Count}.");
            }
            _all = _info.LoadAll(true);
            ApplySearch();
        }
    }
}
