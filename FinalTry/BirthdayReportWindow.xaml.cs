using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HrDepartment
{
    public partial class BirthdayReportWindow : Window
    {
        private readonly ReportsService _reports;
        private List<BirthdayReportRow> _rows = new();

        public BirthdayReportWindow()
        {
            InitializeComponent();
            _reports = new ReportsService(DbConfig.ConnectionString);
            DaysCombo.SelectedIndex = 2;
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            int daysAhead = GetSelectedDays();

            try
            {
                _rows = _reports.GetUpcomingBirthdays(daysAhead);
                Grid.ItemsSource = _rows;

                if (_rows.Count == 0)
                    MessageBox.Show("В выбранный период дней рождения не найдено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчёта: {ex.Message}");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0)
            {
                MessageBox.Show("Сначала сформируйте отчёт.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                FileName = "Отчет_ДниРождения",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != true)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Сотрудник;Подразделение;Должность;Дата рождения;Ближайший день рождения;Через дней");

            foreach (var row in _rows)
            {
                sb.AppendLine($"{Esc(row.EmployeeName)};{Esc(row.DepartmentName)};{Esc(row.PositionName)};{row.BirthDate:dd.MM.yyyy};{row.UpcomingBirthday:dd.MM.yyyy};{row.DaysLeft}");
            }

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Экспорт выполнен.");
        }

        private int GetSelectedDays()
        {
            if (DaysCombo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Tag?.ToString(), out int days))
            {
                return days;
            }

            return 30;
        }

        private static string Esc(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
