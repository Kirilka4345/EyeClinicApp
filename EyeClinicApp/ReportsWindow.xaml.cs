using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace EyeClinicApp
{
    public partial class ReportsWindow : Window
    {
        private DataTable _reportData = new DataTable();

        public ReportsWindow()
        {
            InitializeComponent();
            cmbReportType.SelectedIndex = 0;
        }

        private void CmbReportType_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Автоматическая генерация при смене типа
        }

        /// <summary>
        /// Формирование отчёта
        /// </summary>
        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int reportIndex = cmbReportType.SelectedIndex;

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "";

                    switch (reportIndex)
                    {
                        case 0: // По диагнозам
                            query = @"
                                SELECT d.НаименованиеДиагноза AS [Диагноз], 
                                       COUNT(p.РегНомер) AS [Кол-во пациентов],
                                       STRING_AGG(p.ФИОПациента, ', ') AS [Пациенты]
                                FROM Диагнозы d
                                LEFT JOIN Пациенты p ON d.КодДиагноза = p.КодДиагноза
                                GROUP BY d.НаименованиеДиагноза
                                ORDER BY [Кол-во пациентов] DESC";
                            break;

                        case 1: // По врачам
                            query = @"
                                SELECT v.ФИОВрача AS [Врач], v.Специализация,
                                       COUNT(p.РегНомер) AS [Кол-во пациентов],
                                       STRING_AGG(p.ФИОПациента, ', ') AS [Пациенты]
                                FROM Врачи v
                                LEFT JOIN Пациенты p ON v.КодВрача = p.КодВрача
                                GROUP BY v.ФИОВрача, v.Специализация
                                ORDER BY [Кол-во пациентов] DESC";
                            break;

                        case 2: // По палатам
                            query = @"
                                SELECT p.НомерПалаты AS [Палата],
                                       COUNT(p.РегНомер) AS [Кол-во пациентов],
                                       STRING_AGG(p.ФИОПациента, ', ') AS [Пациенты],
                                       STRING_AGG(d.НаименованиеДиагноза, ', ') AS [Диагнозы]
                                FROM Пациенты p
                                INNER JOIN Диагнозы d ON p.КодДиагноза = d.КодДиагноза
                                GROUP BY p.НомерПалаты
                                ORDER BY p.НомерПалаты";
                            break;
                    }

                    var adapter = new SqlDataAdapter(query, conn);
                    _reportData = new DataTable();
                    adapter.Fill(_reportData);

                    reportGrid.ItemsSource = _reportData.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчёта:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Экспорт в Excel с использованием ClosedXML
        /// </summary>
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_reportData == null || _reportData.Rows.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчёт!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel файлы (*.xlsx)|*.xlsx",
                FileName = $"Отчёт_{DateTime.Now:dd.MM.yyyy_HH-mm}.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Отчёт");

                        // Заголовки
                        for (int j = 0; j < _reportData.Columns.Count; j++)
                        {
                            ws.Cell(1, j + 1).Value = _reportData.Columns[j].ColumnName;
                            ws.Cell(1, j + 1).Style.Font.Bold = true;
                            ws.Cell(1, j + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2B5797");
                            ws.Cell(1, j + 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        }

                        // Данные
                        for (int i = 0; i < _reportData.Rows.Count; i++)
                        {
                            for (int j = 0; j < _reportData.Columns.Count; j++)
                            {
                                ws.Cell(i + 2, j + 1).Value = _reportData.Rows[i][j]?.ToString() ?? "";
                            }
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(dlg.FileName);
                    }

                    MessageBox.Show($"Отчёт успешно экспортирован в Excel!\n{dlg.FileName}", "Экспорт",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Экспорт в PDF с использованием iText7
        /// </summary>
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_reportData == null || _reportData.Rows.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчёт!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF файлы (*.pdf)|*.pdf",
                FileName = $"Отчёт_{DateTime.Now:dd.MM.yyyy_HH-mm}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new iText.Kernel.Pdf.PdfWriter(dlg.FileName))
                    using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                    {
                        var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

                        // Подключение шрифта с поддержкой кириллицы
                        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                        iText.Kernel.Font.PdfFont cyrFont;
                        if (File.Exists(fontPath))
                            cyrFont = iText.Kernel.Font.PdfFontFactory.CreateFont(fontPath, iText.IO.Font.PdfEncodings.IDENTITY_H);
                        else
                            cyrFont = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                        document.SetFont(cyrFont);

                        // Заголовок
                        string reportTitle = (cmbReportType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Отчёт";
                        var title = new iText.Layout.Element.Paragraph(reportTitle)
                            .SetFont(cyrFont)
                            .SetFontSize(16)
                            .SetBold()
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                        document.Add(title);

                        var datePara = new iText.Layout.Element.Paragraph($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .SetFont(cyrFont)
                            .SetFontSize(10)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                        document.Add(datePara);

                        // Таблица
                        var table = new iText.Layout.Element.Table(_reportData.Columns.Count)
                            .UseAllAvailableWidth();

                        // Заголовки
                        for (int j = 0; j < _reportData.Columns.Count; j++)
                        {
                            var cell = new iText.Layout.Element.Cell()
                                .Add(new iText.Layout.Element.Paragraph(_reportData.Columns[j].ColumnName)
                                    .SetFont(cyrFont).SetBold().SetFontSize(9))
                                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(43, 87, 151))
                                .SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE);
                            table.AddHeaderCell(cell);
                        }

                        // Данные
                        for (int i = 0; i < _reportData.Rows.Count; i++)
                        {
                            for (int j = 0; j < _reportData.Columns.Count; j++)
                            {
                                var cell = new iText.Layout.Element.Cell()
                                    .Add(new iText.Layout.Element.Paragraph(
                                        _reportData.Rows[i][j]?.ToString() ?? "")
                                        .SetFont(cyrFont).SetFontSize(8));
                                table.AddCell(cell);
                            }
                        }

                        document.Add(table);
                        document.Close();
                    }

                    MessageBox.Show($"Отчёт успешно экспортирован в PDF!\n{dlg.FileName}", "Экспорт",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта:\n{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
