using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UP01_Smolin_Tournaments.Helpers;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace UP01_Smolin_Tournaments.Pages
{
    /// <summary>
    /// Логика взаимодействия для DiagrammPage.xaml
    /// </summary>
    public partial class DiagrammPage : Page
    {
        private bool _ready;   

        public DiagrammPage()
        {
            InitializeComponent();

            ChartMain.ChartAreas.Add(new ChartArea("Main"));
            ChartMain.Series.Add(new Series("Турниры") { IsValueShownAsLabel = true });

            var organizers = LoadOrganizers();
            organizers.Insert(0, new User { UserID = 0, LastName = "Все организаторы" });
            CbOrganizer.ItemsSource = organizers;

            CbChartType.ItemsSource = new[]
            {
                SeriesChartType.Column, SeriesChartType.Bar, SeriesChartType.Pie, SeriesChartType.Doughnut,
                SeriesChartType.Line, SeriesChartType.Spline, SeriesChartType.Area, SeriesChartType.Point
            };

            CbOrganizer.SelectedIndex = 0;
            CbGroup.SelectedIndex = 0;
            CbMetric.SelectedIndex = 0;
            CbChartType.SelectedIndex = 0;

            _ready = true;
            UpdateChart(null, null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => Session.DenyIfNotAdmin(this);

        private static List<User> LoadOrganizers() =>
            Db.Ctx.Users.Where(u => u.Tournaments.Any()).ToList().OrderBy(u => u.FullName).ToList();

        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready || !(CbChartType.SelectedItem is SeriesChartType)) return;

            List<Tournament> tournaments = Db.Ctx.Tournaments
                .Include("Discipline").Include("TournamentStatus").Include("TournamentFormat")
                .ToList();
            var organizer = CbOrganizer.SelectedItem as User;
            if (organizer != null && organizer.UserID != 0)
                tournaments = tournaments.Where(t => t.OrganizerID == organizer.UserID).ToList();

            List<string> categories;
            Func<Tournament, string> categoryOf;
            switch (CbGroup.SelectedIndex)
            {
                case 1:
                    categories = Db.Ctx.TournamentStatuses.OrderBy(s => s.TournamentStatusID).Select(s => s.Name).ToList();
                    categoryOf = t => t.TournamentStatus.Name;
                    break;
                case 2:
                    categories = Db.Ctx.TournamentFormats.OrderBy(f => f.Name).Select(f => f.Name).ToList();
                    categoryOf = t => t.TournamentFormat.Name;
                    break;
                default:
                    categories = Db.Ctx.Disciplines.OrderBy(d => d.Name).Select(d => d.Name).ToList();
                    categoryOf = t => t.Discipline.Name;
                    break;
            }

            Func<Tournament, decimal> valueOf;
            switch (CbMetric.SelectedIndex)
            {
                case 1: valueOf = t => 1; break;                        
                case 2: valueOf = t => t.PrizePool; break;              
                default: valueOf = t => t.PlannedEntryRevenue; break;   
            }

            Series series = ChartMain.Series[0];
            series.ChartType = (SeriesChartType)CbChartType.SelectedItem;
            series.Points.Clear();
            foreach (string category in categories)
            {
                decimal sum = tournaments.Where(t => categoryOf(t) == category).Sum(valueOf);
                series.Points.AddXY(category, (double)sum);
            }

            string metric = (CbMetric.SelectedItem as ComboBoxItem)?.Content.ToString();
            string group = (CbGroup.SelectedItem as ComboBoxItem)?.Content.ToString();
            TbChartTitle.Text = $"{metric} по {group} — {organizer?.FullName ?? "Все организаторы"}";
        }
        private void BtnExcel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Книга Excel (*.xlsx)|*.xlsx",
                FileName = "Турниры_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            List<User> organizers = LoadOrganizers();
            if (organizers.Count == 0) { Msg.Info("Нет турниров для выгрузки."); return; }

            Excel.Application application = null;
            try
            {
                application = new Excel.Application();
                int oldSheetsCount = application.SheetsInNewWorkbook;
                application.SheetsInNewWorkbook = organizers.Count;
                Excel.Workbook workbook = application.Workbooks.Add(Type.Missing);
                application.SheetsInNewWorkbook = oldSheetsCount; 

                decimal grandTotal = 0;
                string[] headers = { "Дата начала", "Турнир", "Взнос, ₽", "Команд (макс.)",
                                     "Плановый сбор, ₽", "Статус", "Место" };

                for (int i = 0; i < organizers.Count; i++)
                {
                    User organizer = organizers[i];
                    Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets[i + 1];
                    sheet.Name = SheetName(organizer.ShortName, i);
                    int row = 1; 

                    for (int c = 0; c < headers.Length; c++)
                        sheet.Cells[row, c + 1] = headers[c];
                    Excel.Range headerRange = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, headers.Length]];
                    headerRange.Font.Bold = true;
                    headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                    row++;

                    var groups = organizer.Tournaments
                        .OrderBy(t => t.StartDate)
                        .GroupBy(t => t.Discipline.Name)
                        .OrderBy(g => g.Key);

                    foreach (var group in groups)
                    {
                        Excel.Range groupRange = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, headers.Length]];
                        groupRange.Merge();
                        groupRange.Value2 = group.Key;
                        groupRange.Font.Italic = true;
                        groupRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                        row++;

                        int firstRow = row;
                        foreach (Tournament t in group)
                        {
                            sheet.Cells[row, 1] = t.StartDate.Date; 
                            ((Excel.Range)sheet.Cells[row, 1]).NumberFormat = "dd.mm.yyyy";
                            sheet.Cells[row, 2] = t.Name;
                            sheet.Cells[row, 3] = (double)t.EntryFee;
                            sheet.Cells[row, 4] = t.MaxTeams;
                            sheet.Cells[row, 5].Formula = $"=C{row}*D{row}";  
                            sheet.Cells[row, 6] = t.TournamentStatus.Name;
                            sheet.Cells[row, 7] = t.PlaceText;
                            ((Excel.Range)sheet.Cells[row, 3]).NumberFormat = "0.00";
                            ((Excel.Range)sheet.Cells[row, 5]).NumberFormat = "0.00";
                            grandTotal += t.PlannedEntryRevenue;
                            row++;
                        }

                        Excel.Range totalLabel = sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, 4]];
                        totalLabel.Merge();
                        totalLabel.Value2 = "ИТОГО:";
                        totalLabel.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
                        totalLabel.Font.Bold = true;
                        sheet.Cells[row, 5].Formula = $"=SUM(E{firstRow}:E{row - 1})";
                        ((Excel.Range)sheet.Cells[row, 5]).NumberFormat = "0.00";
                        ((Excel.Range)sheet.Cells[row, 5]).Font.Bold = true;
                        row++;
                    }

                    Excel.Range table = sheet.Range[sheet.Cells[1, 1], sheet.Cells[row - 1, headers.Length]];
                    foreach (Excel.XlBordersIndex edge in new[]
                    {
                        Excel.XlBordersIndex.xlEdgeLeft, Excel.XlBordersIndex.xlEdgeTop,
                        Excel.XlBordersIndex.xlEdgeRight, Excel.XlBordersIndex.xlEdgeBottom,
                        Excel.XlBordersIndex.xlInsideHorizontal, Excel.XlBordersIndex.xlInsideVertical
                    })
                        table.Borders[edge].LineStyle = Excel.XlLineStyle.xlContinuous;
                    sheet.Columns.AutoFit();
                }

                Excel.Worksheet summary = (Excel.Worksheet)workbook.Worksheets.Add(
                    After: workbook.Worksheets[workbook.Worksheets.Count]);
                summary.Name = "Общий итог";
                summary.Cells[1, 1] = "Общий итог (плановый сбор взносов), ₽:";
                summary.Cells[1, 2] = (double)grandTotal;
                ((Excel.Range)summary.Cells[1, 2]).NumberFormat = "0.00";
                Excel.Range summaryRange = summary.Range[summary.Cells[1, 1], summary.Cells[1, 2]];
                summaryRange.Font.Color = Excel.XlRgbColor.rgbRed;
                summaryRange.Font.Bold = true;
                summary.Columns.AutoFit();

                application.DisplayAlerts = false;
                workbook.SaveAs(dialog.FileName);
                application.DisplayAlerts = true;
                application.Visible = true;

                Audit.Write("EXPORT", "Tournaments");
                Msg.Info("Книга Excel сохранена:\n" + dialog.FileName);
            }
            catch (Exception ex)
            {
                CloseExcel(application);
                Msg.Error("Не удалось выполнить экспорт в Excel. Проверьте, что установлен Microsoft Excel.\n\n" + ex.Message);
            }
        }

        private static string SheetName(string name, int index)
        {
            foreach (char ch in new[] { '[', ']', ':', '*', '?', '/', '\\' })
                name = name.Replace(ch, ' ');
            string suffix = " " + (index + 1);   
            if (name.Length + suffix.Length > 31)
                name = name.Substring(0, 31 - suffix.Length);
            return name + suffix;
        }

        private static void CloseExcel(Excel.Application application)
        {
            if (application == null) return;
            try
            {
                application.DisplayAlerts = false;
                ((Excel._Application)application).Quit();
            }
            catch { }
        }

        private void BtnWord_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Документ Word (*.docx)|*.docx",
                FileName = "Отчёт_по_турнирам_" + DateTime.Now.ToString("yyyy-MM-dd") + ".docx"
            };
            if (dialog.ShowDialog() != true) return;
            string docxPath = dialog.FileName;
            string pdfPath = System.IO.Path.ChangeExtension(docxPath, ".pdf");

            List<User> organizers = LoadOrganizers();
            if (organizers.Count == 0) { Msg.Info("Нет турниров для выгрузки."); return; }

            Word.Application application = null;
            try
            {
                application = new Word.Application();
                Word.Document document = application.Documents.Add();

                for (int i = 0; i < organizers.Count; i++)
                {
                    User organizer = organizers[i];
                    List<Tournament> tournaments = organizer.Tournaments.OrderBy(t => t.StartDate).ToList();

                    Word.Paragraph userParagraph = document.Paragraphs.Add();
                    Word.Range userRange = userParagraph.Range;
                    userRange.Text = "Организатор: " + organizer.FullName;
                    object headingStyle = Word.WdBuiltinStyle.wdStyleHeading1;
                    userParagraph.set_Style(ref headingStyle);
                    userRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    userRange.InsertParagraphAfter();

                    Word.Paragraph tableParagraph = document.Paragraphs.Add();
                    object normalStyle = Word.WdBuiltinStyle.wdStyleNormal; 
                    tableParagraph.set_Style(ref normalStyle);
                    tableParagraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                    Word.Range tableRange = tableParagraph.Range;
                    Word.Table table = document.Tables.Add(tableRange, tournaments.Count + 1, 5);
                    table.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    table.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    table.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                    string[] headers = { "Дата начала", "Турнир", "Дисциплина", "Статус", "Плановый сбор, ₽" };
                    for (int c = 0; c < headers.Length; c++)
                    {
                        Word.Range cell = table.Cell(1, c + 1).Range;
                        cell.Text = headers[c];
                        cell.Bold = 1;
                        cell.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    }
                    for (int r = 0; r < tournaments.Count; r++)
                    {
                        Tournament t = tournaments[r];
                        table.Cell(r + 2, 1).Range.Text = t.StartDate.ToString("dd.MM.yyyy");
                        table.Cell(r + 2, 2).Range.Text = t.Name;
                        table.Cell(r + 2, 3).Range.Text = t.Discipline.Name;
                        table.Cell(r + 2, 4).Range.Text = t.TournamentStatus.Name;
                        table.Cell(r + 2, 5).Range.Text = t.PlannedEntryRevenue.ToString("N2");
                    }

                    Tournament maxFee = tournaments.OrderByDescending(t => t.EntryFee).First();
                    Tournament minFee = tournaments.OrderBy(t => t.EntryFee).First();
                    AddLine(document, $"Самый высокий взнос: {maxFee.Name} — {maxFee.EntryFee:N2} ₽",
                            Word.WdColor.wdColorDarkRed);
                    AddLine(document, $"Самый низкий взнос: {minFee.Name} — {minFee.EntryFee:N2} ₽",
                            Word.WdColor.wdColorDarkGreen);
                    AddLine(document, $"Итого плановый сбор: {tournaments.Sum(t => t.PlannedEntryRevenue):N2} ₽",
                            Word.WdColor.wdColorAutomatic);

                    if (i < organizers.Count - 1)
                        document.Words.Last.InsertBreak(Word.WdBreakType.wdPageBreak);
                }

                foreach (Word.Section section in document.Sections)
                {
                    Word.Range header = section.Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                    header.Text = "Отчёт сформирован: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    header.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    section.Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary]
                           .PageNumbers.Add(Word.WdPageNumberAlignment.wdAlignPageNumberCenter);
                }

                application.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;
                document.SaveAs2(docxPath);
                document.ExportAsFixedFormat(pdfPath, Word.WdExportFormat.wdExportFormatPDF);
                application.Visible = true;

                Audit.Write("EXPORT", "Tournaments");
                Msg.Info("Документы сохранены:\n" + docxPath + "\n" + pdfPath);
            }
            catch (Exception ex)
            {
                if (application != null)
                {
                    try { ((Word._Application)application).Quit(false); } catch { }
                }
                Msg.Error("Не удалось выполнить экспорт в Word. Проверьте, что установлен Microsoft Word.\n\n" + ex.Message);
            }
        }

        private static void AddLine(Word.Document document, string text, Word.WdColor color)
        {
            Word.Paragraph paragraph = document.Paragraphs.Add();
            object normalStyle = Word.WdBuiltinStyle.wdStyleNormal;
            paragraph.set_Style(ref normalStyle);
            paragraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            Word.Range range = paragraph.Range;
            range.Text = text;
            range.Font.Color = color;
            range.InsertParagraphAfter();
        }
    }
}
