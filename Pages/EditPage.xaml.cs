using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для EditPage.xaml
    /// </summary>
    public partial class EditPage : Page
    {
        private readonly IFormConfig _config;
        private readonly List<Control> _inputs = new List<Control>(); 

        public EditPage(IFormConfig config)
        {
            InitializeComponent();
            _config = config;
            Title = config.Title;   
            TbHeader.Text = Title;

            string[] values = config.LoadValues();
            for (int i = 0; i < config.Fields.Count; i++)
                AddField(config.Fields[i], values[i]);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Session.DenyIfNotAdmin(this);

        private void AddField(FormField f, string value)
        {
            string label = f.Label + (f.Required ? " *" : "");
            if (f.Kind == FieldKind.Int) label += $" (от {f.Min} до {f.Max})";
            FieldsPanel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(5, 8, 5, 0) });

            if (f.Kind == FieldKind.Choice)
            {
                var combo = new ComboBox { IsEditable = true, ToolTip = f.ToolTip, ItemsSource = f.Choices, Text = value ?? "" };
                FieldsPanel.Children.Add(combo);
                _inputs.Add(combo);
                return;
            }

            var box = new TextBox { ToolTip = f.ToolTip };
            if (f.Kind == FieldKind.MultiLine)
            {
                box.Height = 70;
                box.TextWrapping = TextWrapping.Wrap;
                box.AcceptsReturn = true;
                box.VerticalContentAlignment = VerticalAlignment.Top;
            }
            if (f.Kind == FieldKind.Int)
            {
                box.MaxLength = f.Max.ToString().Length; 
                InputMask.Digits(box);
            }
            else
            {
                box.MaxLength = f.Max;
            }
            if (f.Mask != null) f.Mask(box);

            var placeholder = new TextBlock { Text = f.Placeholder ?? "", Style = (Style)FindResource("Placeholder") };
            if (f.Kind == FieldKind.MultiLine)
            {
                placeholder.VerticalAlignment = VerticalAlignment.Top;
                placeholder.Margin = new Thickness(13, 10, 0, 0);
            }
            box.Tag = placeholder;
            box.Text = value ?? "";        

            var grid = new Grid();
            grid.Children.Add(box);
            grid.Children.Add(placeholder);
            FieldsPanel.Children.Add(grid);
            _inputs.Add(box);
            Ui.UpdatePlaceholder(box, box.Text.Length > 0);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var v = new FormValidator();
            var values = new object[_config.Fields.Count];

            for (int i = 0; i < _config.Fields.Count; i++)
            {
                FormField f = _config.Fields[i];
                var combo = _inputs[i] as ComboBox;
                string text = combo != null ? combo.Text : ((TextBox)_inputs[i]).Text;

                if (f.Kind == FieldKind.Int)
                {
                    values[i] = v.Int(text, f.Label, f.Min, f.Max);
                }
                else
                {
                    string s = v.Text(text, f.Label, f.Min, f.Max, f.Required);
                    if (s != null && f.Pattern != null)
                        v.Require(Regex.IsMatch(s, f.Pattern), f.PatternError);
                    values[i] = s;
                }
            }

            if (!v.HasErrors)
            {
                string duplicate = _config.CheckUnique(values);
                v.Require(duplicate == null, duplicate);
            }
            if (v.ShowErrors()) return;

            if (_config.Save(values))
            {
                Msg.Info("Запись сохранена.");
                NavigationService?.GoBack();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => NavigationService?.GoBack();
    }
}
