using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HrDepartment
{
    public partial class DepartmentCountReportWindow : Window
    {
        private readonly ReportsService _reports;
        private List<DepartmentCountReportRow> _rows = new();

        public DepartmentCountReportWindow()
        {
            InitializeComponent();
            _reports = new ReportsService(DbConfig.ConnectionString);
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _rows = _reports.GetEmployeesCountByDepartments();
                Grid.ItemsSource = _rows;

                if (_rows.Count == 0)
                    MessageBox.Show("Нет данных для отчёта.");
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
                FileName = "Отчет_Подразделения",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("Подразделение;Кол-во сотрудников");

            foreach (var r in _rows)
            {
                sb.AppendLine($"{Esc(r.DepartmentName)};{r.EmployeesCount}");
            }

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Экспорт выполнен.");
        }

        private static string Esc(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
