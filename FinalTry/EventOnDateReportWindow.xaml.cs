using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace HrDepartment
{
    public partial class EventOnDateReportWindow : Window
    {
        private readonly ReportsService _reports;
        private readonly EventTypeService _typesService;
        private List<EventOnDateReportRow> _rows = new();

        public EventOnDateReportWindow()
        {
            InitializeComponent();

            _reports = new ReportsService(DbConfig.ConnectionString);
            _typesService = new EventTypeService(DbConfig.ConnectionString);

            DatePicker.SelectedDate = DateTime.Today;

            var types = _typesService.GetAll();
            TypeCombo.ItemsSource = types;

            // Поставим дефолт: "Отпускной" если есть
            var defaultType = types.FirstOrDefault(t => t.Name.Equals("Отпускной", StringComparison.OrdinalIgnoreCase))
                              ?? types.FirstOrDefault();
            TypeCombo.SelectedItem = defaultType;
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker.SelectedDate is null)
            {
                MessageBox.Show("Выберите дату.");
                return;
            }

            if (TypeCombo.SelectedItem is not EventType type)
            {
                MessageBox.Show("Выберите тип события.");
                return;
            }

            try
            {
                _rows = _reports.GetEmployeesByEventOnDate(DatePicker.SelectedDate.Value, type.Id);
                Grid.ItemsSource = _rows;

                if (_rows.Count == 0)
                    MessageBox.Show("На выбранную дату записей не найдено.");
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
                FileName = "Отчет_События_На_Дату",
                DefaultExt = ".csv",
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("Сотрудник;Подразделение;Должность;С;По;Обоснование");

            foreach (var r in _rows)
            {
                sb.AppendLine($"{Esc(r.EmployeeName)};{Esc(r.DepartmentName)};{Esc(r.PositionName)};{r.StartDate:dd.MM.yyyy};{r.EndDate:dd.MM.yyyy};{Esc(r.Reason)}");
            }

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Экспорт выполнен.");
        }

        private static string Esc(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // CSV через ; — просто экранируем переносы
            return s.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
