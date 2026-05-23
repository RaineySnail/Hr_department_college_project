using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Windows;

namespace HrDepartment
{
    public partial class VacationRequestWindow : Window
    {
        private readonly Employee _employee;

        public VacationRequestWindow(Employee employee)
        {
            InitializeComponent();
            _employee = employee;
            EmployeeNameText.Text = $"Сотрудник: {employee.FullName}";
            SignatureTextBox.Text = employee.FullName;
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(14);
            SavePathInfoText.Text = $"3. Excel-документ будет сохранён на рабочем столе в папке {GetVacationFolderPath()}.";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (startDate is null || endDate is null)
            {
                MessageBox.Show("Укажите даты отпуска.");
                return;
            }

            if (endDate < startDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала.");
                return;
            }

            if (!PersonalDataConsentCheckBox.IsChecked.GetValueOrDefault() ||
                !VacationConsentCheckBox.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show("Нужно подтвердить оба согласия.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SignatureTextBox.Text))
            {
                MessageBox.Show("Укажите подпись сотрудника.");
                return;
            }

            using var connection = new MySqlConnection(DbConfig.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using (var insertEvent = new MySqlCommand(@"
INSERT INTO событие (id_сотрудника, `Дата_начала`, `Дата окончания`, `Обоснование`, id_типа_события)
VALUES (@employeeId, @startDate, @endDate, @reason, 3);", connection, transaction))
                {
                    insertEvent.Parameters.AddWithValue("@employeeId", _employee.Id);
                    insertEvent.Parameters.AddWithValue("@startDate", startDate.Value.Date);
                    insertEvent.Parameters.AddWithValue("@endDate", endDate.Value.Date);
                    insertEvent.Parameters.AddWithValue("@reason", ReasonTextBox.Text.Trim());
                    insertEvent.ExecuteNonQuery();
                }

                using (var insertDoc = new MySqlCommand(@"
INSERT INTO документ_отпуска (id_сотрудника, date_start, date_end, reason, employee_signature, consent_personal_data, consent_vacation)
VALUES (@employeeId, @startDate, @endDate, @reason, @signature, 1, 1);", connection, transaction))
                {
                    insertDoc.Parameters.AddWithValue("@employeeId", _employee.Id);
                    insertDoc.Parameters.AddWithValue("@startDate", startDate.Value.Date);
                    insertDoc.Parameters.AddWithValue("@endDate", endDate.Value.Date);
                    insertDoc.Parameters.AddWithValue("@reason", ReasonTextBox.Text.Trim());
                    insertDoc.Parameters.AddWithValue("@signature", SignatureTextBox.Text.Trim());
                    insertDoc.ExecuteNonQuery();
                }

                using (var updateFlag = new MySqlCommand(@"
UPDATE сотрудник
SET В_отпуске = CASE WHEN CURDATE() BETWEEN @startDate AND @endDate THEN 1 ELSE 0 END
WHERE id_employee = @employeeId;", connection, transaction))
                {
                    updateFlag.Parameters.AddWithValue("@startDate", startDate.Value.Date);
                    updateFlag.Parameters.AddWithValue("@endDate", endDate.Value.Date);
                    updateFlag.Parameters.AddWithValue("@employeeId", _employee.Id);
                    updateFlag.ExecuteNonQuery();
                }

                transaction.Commit();
                CreateExcelDocument(startDate.Value, endDate.Value);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Не удалось оформить отпуск.\n{ex.Message}");
            }
        }

        private void CreateExcelDocument(DateTime startDate, DateTime endDate)
        {
            string folder = GetVacationFolderPath();
            Directory.CreateDirectory(folder);
            string safeName = string.Join("_", _employee.FullName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Replace(' ', '_');
            string filePath = Path.Combine(folder, $"Отпуск_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Отпуск");
            ws.Cell("A1").Value = "ЗАЯВЛЕНИЕ НА ОТПУСК";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Range("A1:D1").Merge();

            ws.Cell("A3").Value = "ФИО сотрудника";
            ws.Cell("B3").Value = _employee.FullName;
            ws.Cell("A4").Value = "Подразделение";
            ws.Cell("B4").Value = _employee.DepartmentName;
            ws.Cell("A5").Value = "Должность";
            ws.Cell("B5").Value = _employee.PositionName;
            ws.Cell("A6").Value = "Дата начала отпуска";
            ws.Cell("B6").Value = startDate;
            ws.Cell("B6").Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Cell("A7").Value = "Дата окончания отпуска";
            ws.Cell("B7").Value = endDate;
            ws.Cell("B7").Style.DateFormat.Format = "dd.MM.yyyy";
            ws.Cell("A8").Value = "Основание";
            ws.Cell("B8").Value = ReasonTextBox.Text.Trim();
            ws.Cell("A10").Value = "Согласие на обработку ПДн";
            ws.Cell("B10").Value = "Да";
            ws.Cell("A11").Value = "Согласие на отпуск";
            ws.Cell("B11").Value = "Да";
            ws.Cell("A13").Value = "Подпись сотрудника";
            ws.Cell("B13").Value = SignatureTextBox.Text.Trim();
            ws.Cell("A14").Value = "Дата оформления";
            ws.Cell("B14").Value = DateTime.Today;
            ws.Cell("B14").Style.DateFormat.Format = "dd.MM.yyyy";

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);

            MessageBox.Show($"Отпуск оформлен. Документ сохранён:\n{filePath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetVacationFolderPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return Path.Combine(desktopPath, "HR Department", "Отпуска");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
