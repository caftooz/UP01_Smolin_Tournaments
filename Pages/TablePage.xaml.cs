using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UP01_Smolin_Tournaments.Helpers;
using UP01_Smolin_Tournaments.Helpers.Crud;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для TablePage.xaml
    /// </summary>
    public partial class TablePage : Page
    {
        private readonly ITableConfig _config;  
        private bool _ready;                   

        public TablePage(ITableConfig config)
        {
            InitializeComponent();
            _config = config;

            Title = config.Title;                
            PhSearch.Text = config.SearchHint;
            TbSearch.ToolTip = config.SearchHint;

            if (config.FilterHint == null)
                CbFilter.Visibility = Visibility.Collapsed;
            else
                CbFilter.ToolTip = config.FilterHint;

            BuildColumns();
        }

        private void BuildColumns()
        {
            var widthConverter = new DataGridLengthConverter();
            foreach (ColumnInfo c in _config.Columns)
            {
                var binding = new Binding(c.Path);
                if (c.Format != null) binding.StringFormat = c.Format;

                DataGridBoundColumn column;
                if (c.IsCheckBox) column = new DataGridCheckBoxColumn();
                else column = new DataGridTextColumn();

                column.Header = c.Header;
                column.Binding = binding;
                column.Width = (DataGridLength)widthConverter.ConvertFromString(c.Width);
                GridItems.Columns.Add(column);
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || Session.DenyIfNotAdmin(this)) return;

            _ready = false;
            if (_config.FilterHint != null)
            {
                int selectedId = (CbFilter.SelectedItem as FilterOption)?.Id ?? 0;
                var options = _config.LoadFilter();
                options.Insert(0, new FilterOption { Id = 0, Name = _config.FilterAllText });
                CbFilter.ItemsSource = options;
                CbFilter.SelectedItem = options.FirstOrDefault(o => o.Id == selectedId) ?? options[0];
            }
            _ready = true;
            LoadData();
        }

        private void LoadData()
        {
            if (!_ready) return;
            int filterId = (CbFilter.SelectedItem as FilterOption)?.Id ?? 0;
            IList items = _config.Load(TbSearch.Text, filterId);
            GridItems.ItemsSource = items;
            TbCount.Text = "Записей: " + items.Count;
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadData();
        private void CbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e) =>
            NavigationService?.Navigate(_config.CreateEditor(null));

        private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();
        private void GridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(GridItems, e.OriginalSource as DependencyObject) is DataGridRow)
                EditSelected();
        }

        private void EditSelected()
        {
            if (GridItems.SelectedItem == null) { Msg.Info("Выделите запись в таблице."); return; }
            NavigationService?.Navigate(_config.CreateEditor(_config.IdOf(GridItems.SelectedItem)));
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var ids = GridItems.SelectedItems.Cast<object>().Select(x => _config.IdOf(x)).ToList();
            if (ids.Count == 0) { Msg.Info("Выделите записи, которые нужно удалить."); return; }
            _config.Delete(ids);
            LoadData();
        }
    }
}
