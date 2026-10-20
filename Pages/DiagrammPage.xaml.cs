using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
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
        private const string MoneyFormat = "0.00";
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
                series.Points.AddXY(category, (double)tournaments.Where(t => categoryOf(t) == category).Sum(valueOf));

            string metric = (CbMetric.SelectedItem as ComboBoxItem)?.Content.ToString();
            string group = (CbGroup.SelectedItem as ComboBoxItem)?.Content.ToString();
            TbChartTitle.Text = $"{metric} по {group} — {organizer?.FullName ?? "Все организаторы"}";
        }

        private void BtnExcel_Click(object sender, RoutedEventArgs e) =>
            Export("Книга Excel (*.xlsx)|*.xlsx", "Турниры_{0}.xlsx", "Excel", () => new Excel.Application(),
                   app => { app.DisplayAlerts = false; ((Excel._Application)app).Quit(); }, ExportExcel);

        private void BtnWord_Click(object sender, RoutedEventArgs e) =>
            Export("Документ Word (*.docx)|*.docx", "Отчёт_по_турнирам_{0}.docx", "Word", () => new Word.Application(),
                   app => ((Word._Application)app).Quit(false), ExportWord);

        private static void Export<TApp>(string filter, string fileName, string program, Func<TApp> start,
                                         Action<TApp> quit, Func<TApp, string, List<User>, string> export)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = string.Format(fileName, DateTime.Now.ToString("yyyy-MM-dd"))
            };
            if (dialog.ShowDialog() != true) return;

            List<User> organizers = LoadOrganizers();
            if (organizers.Count == 0) { Msg.Info("Нет турниров для выгрузки."); return; }

            TApp application = default(TApp);
            try
            {
                application = start();
                string result = export(application, dialog.FileName, organizers);
                Audit.Write("EXPORT", "Tournaments");
                Msg.Info(result);
            }
            catch (Exception ex)
            {
                if (application != null)
                {
                    try { quit(application); } catch { }
                }
                Msg.Error($"Не удалось выполнить экспорт в {program}. Проверьте, что установлен Microsoft {program}.\n\n" + ex.Message);
            }
        }

        private static string ExportExcel(Excel.Application application, string path, List<User> organizers)
        {
            int oldSheetsCount = application.SheetsInNewWorkbook;
            application.SheetsInNewWorkbook = organizers.Count;
            Excel.Workbook workbook = application.Workbooks.Add(Type.Missing);
            application.SheetsInNewWorkbook = oldSheetsCount;

            decimal grandTotal = 0;
            for (int i = 0; i < organizers.Count; i++)
            {
                Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets[i + 1];
                sheet.Name = SheetName(organizers[i].ShortName, i);
                grandTotal += FillOrganizerSheet(sheet, organizers[i]);
            }

            Excel.Worksheet summary = (Excel.Worksheet)workbook.Worksheets.Add(
                After: workbook.Worksheets[workbook.Worksheets.Count]);
            summary.Name = "Общий итог";
            Put(summary, 1, 1, "Общий итог (плановый сбор взносов), ₽:");
            Put(summary, 1, 2, (double)grandTotal, MoneyFormat);
            Excel.Range summaryRange = Row(summary, 1, 2);
            summaryRange.Font.Color = Excel.XlRgbColor.rgbRed;
            summaryRange.Font.Bold = true;
            summary.Columns.AutoFit();

            application.DisplayAlerts = false;
            workbook.SaveAs(path);
            application.DisplayAlerts = true;
            application.Visible = true;
            return "Книга Excel сохранена:\n" + path;
        }

        private static decimal FillOrganizerSheet(Excel.Worksheet sheet, User organizer)
        {
            string[] headers = { "Дата начала", "Турнир", "Взнос, ₽", "Команд (макс.)",
                                 "Плановый сбор, ₽", "Статус", "Место" };
            for (int c = 0; c < headers.Length; c++)
                Put(sheet, 1, c + 1, headers[c]);
            Excel.Range headerRange = Row(sheet, 1, headers.Length);
            headerRange.Font.Bold = true;
            headerRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

            int row = 2;
            decimal total = 0;
            var groups = organizer.Tournaments
                .OrderBy(t => t.StartDate)
                .GroupBy(t => t.Discipline.Name)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                MergeRow(sheet, row++, headers.Length, group.Key, Excel.XlHAlign.xlHAlignCenter).Font.Italic = true;

                int firstRow = row;
                foreach (Tournament t in group)
                {
                    Put(sheet, row, 1, t.StartDate.Date, "dd.mm.yyyy");
                    Put(sheet, row, 2, t.Name);
                    Put(sheet, row, 3, (double)t.EntryFee, MoneyFormat);
                    Put(sheet, row, 4, t.MaxTeams);
                    Put(sheet, row, 5, $"=C{row}*D{row}", MoneyFormat);
                    Put(sheet, row, 6, t.TournamentStatus.Name);
                    Put(sheet, row, 7, t.PlaceText);
                    total += t.PlannedEntryRevenue;
                    row++;
                }

                MergeRow(sheet, row, 4, "ИТОГО:", Excel.XlHAlign.xlHAlignRight).Font.Bold = true;
                Put(sheet, row, 5, $"=SUM(E{firstRow}:E{row - 1})", MoneyFormat).Font.Bold = true;
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
            return total;
        }

        private static Excel.Range Row(Excel.Worksheet sheet, int row, int lastColumn) =>
            sheet.Range[sheet.Cells[row, 1], sheet.Cells[row, lastColumn]];

        private static Excel.Range Put(Excel.Worksheet sheet, int row, int column, object value, string format = null)
        {
            var cell = (Excel.Range)sheet.Cells[row, column];
            string text = value as string;
            if (text != null && text.StartsWith("=")) cell.Formula = text;
            else sheet.Cells[row, column] = value;
            if (format != null) cell.NumberFormat = format;
            return cell;
        }

        private static Excel.Range MergeRow(Excel.Worksheet sheet, int row, int lastColumn, string text, Excel.XlHAlign align)
        {
            Excel.Range range = Row(sheet, row, lastColumn);
            range.Merge();
            range.Value2 = text;
            range.HorizontalAlignment = align;
            return range;
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

        private static string ExportWord(Word.Application application, string docxPath, List<User> organizers)
        {
            string pdfPath = System.IO.Path.ChangeExtension(docxPath, ".pdf");
            Word.Document document = application.Documents.Add();
            for (int i = 0; i < organizers.Count; i++)
            {
                AddOrganizerSection(document, organizers[i]);
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
            return "Документы сохранены:\n" + docxPath + "\n" + pdfPath;
        }

        private static void AddOrganizerSection(Word.Document document, User organizer)
        {
            List<Tournament> tournaments = organizer.Tournaments.OrderBy(t => t.StartDate).ToList();

            Word.Paragraph userParagraph = document.Paragraphs.Add();
            Word.Range userRange = userParagraph.Range;
            userRange.Text = "Организатор: " + organizer.FullName;
            SetStyle(userParagraph, Word.WdBuiltinStyle.wdStyleHeading1);
            userRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            userRange.InsertParagraphAfter();

            Word.Table table = document.Tables.Add(NormalParagraph(document).Range, tournaments.Count + 1, 5);
            table.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

            FillRow(table, 1, "Дата начала", "Турнир", "Дисциплина", "Статус", "Плановый сбор, ₽");
            table.Rows[1].Range.Bold = 1;
            table.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            for (int r = 0; r < tournaments.Count; r++)
            {
                Tournament t = tournaments[r];
                FillRow(table, r + 2, t.StartDate.ToString("dd.MM.yyyy"), t.Name, t.Discipline.Name,
                        t.TournamentStatus.Name, t.PlannedEntryRevenue.ToString("N2"));
            }

            Tournament maxFee = tournaments.OrderByDescending(t => t.EntryFee).First();
            Tournament minFee = tournaments.OrderBy(t => t.EntryFee).First();
            AddLine(document, $"Самый высокий взнос: {maxFee.Name} — {maxFee.EntryFee:N2} ₽", Word.WdColor.wdColorDarkRed);
            AddLine(document, $"Самый низкий взнос: {minFee.Name} — {minFee.EntryFee:N2} ₽", Word.WdColor.wdColorDarkGreen);
            AddLine(document, $"Итого плановый сбор: {tournaments.Sum(t => t.PlannedEntryRevenue):N2} ₽", Word.WdColor.wdColorAutomatic);
        }

        private static void FillRow(Word.Table table, int row, params string[] values)
        {
            for (int c = 0; c < values.Length; c++)
                table.Cell(row, c + 1).Range.Text = values[c];
        }

        private static void SetStyle(Word.Paragraph paragraph, Word.WdBuiltinStyle style)
        {
            object styleObject = style;
            paragraph.set_Style(ref styleObject);
        }

        private static Word.Paragraph NormalParagraph(Word.Document document)
        {
            Word.Paragraph paragraph = document.Paragraphs.Add();
            SetStyle(paragraph, Word.WdBuiltinStyle.wdStyleNormal);
            paragraph.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            return paragraph;
        }

        private static void AddLine(Word.Document document, string text, Word.WdColor color)
        {
            Word.Range range = NormalParagraph(document).Range;
            range.Text = text;
            range.Font.Color = color;
            range.InsertParagraphAfter();
        }
    }
}
