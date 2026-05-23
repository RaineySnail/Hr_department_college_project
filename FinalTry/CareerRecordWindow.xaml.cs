using MySql.Data.MySqlClient;
using System;
using System.ComponentModel;
using System.Windows;

namespace HrDepartment
{
    public partial class CareerRecordWindow : Window, INotifyPropertyChanged
    {
        private CareerHistory _currentRecord = new();
        private readonly string _connectionString = DbConfig.ConnectionString;
        private readonly bool _isEditMode;

        public CareerHistory CurrentRecord
        {
            get => _currentRecord;
            set
            {
                _currentRecord = value;
                OnPropertyChanged(nameof(CurrentRecord));
            }
        }

        public string WindowTitle => _isEditMode ? "Редактирование записи" : "Добавление записи";

        public CareerRecordWindow(Employee employee, CareerHistory? record = null)
        {
            InitializeComponent();

            if (record is null)
            {
                _isEditMode = false;
                CurrentRecord = new CareerHistory
                {
                    EmployeeId = employee.Id,
                    StartDate = DateTime.Today
                };
            }
            else
            {
                _isEditMode = true;
                CurrentRecord = record;
            }

            DataContext = this;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                if (_isEditMode)
                {
                    const string updateQuery = @"UPDATE история_карьеры 
                                                SET id_отдела = @departmentId, 
                                                    id_должности = @positionId,
                                                    дата_начала = @startDate,
                                                    дата_окончания = @endDate,
                                                    зарплата = @salary,
                                                    примечание = @note
                                                WHERE id_записи = @recordId";

                    using var command = new MySqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@departmentId", CurrentRecord.DepartmentId);
                    command.Parameters.AddWithValue("@positionId", CurrentRecord.PositionId);
                    command.Parameters.AddWithValue("@startDate", CurrentRecord.StartDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@endDate", CurrentRecord.EndDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@salary", CurrentRecord.Salary);
                    command.Parameters.AddWithValue("@note", CurrentRecord.Note ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@recordId", CurrentRecord.Id);
                    command.ExecuteNonQuery();
                }
                else
                {
                    const string insertQuery = @"INSERT INTO история_карьеры 
                                                (id_сотрудника, id_отдела, id_должности, дата_начала, дата_окончания, зарплата, примечание)
                                                VALUES (@employeeId, @departmentId, @positionId, @startDate, @endDate, @salary, @note)";

                    using var command = new MySqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@employeeId", CurrentRecord.EmployeeId);
                    command.Parameters.AddWithValue("@departmentId", CurrentRecord.DepartmentId);
                    command.Parameters.AddWithValue("@positionId", CurrentRecord.PositionId);
                    command.Parameters.AddWithValue("@startDate", CurrentRecord.StartDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@endDate", CurrentRecord.EndDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@salary", CurrentRecord.Salary);
                    command.Parameters.AddWithValue("@note", CurrentRecord.Note ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            if (CurrentRecord.DepartmentId <= 0)
            {
                MessageBox.Show("ID отдела должен быть положительным числом!");
                return false;
            }

            if (CurrentRecord.PositionId <= 0)
            {
                MessageBox.Show("ID должности должен быть положительным числом!");
                return false;
            }

            if (CurrentRecord.StartDate == DateTime.MinValue)
            {
                MessageBox.Show("Укажите дату начала!");
                return false;
            }

            if (CurrentRecord.EndDate.HasValue && CurrentRecord.EndDate < CurrentRecord.StartDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала!");
                return false;
            }

            if (!CurrentRecord.EndDate.HasValue)
            {
                try
                {
                    using var connection = new MySqlConnection(_connectionString);
                    connection.Open();

                    const string checkQuery = @"SELECT COUNT(*) FROM история_карьеры 
                                                WHERE id_сотрудника = @employeeId 
                                                  AND дата_окончания IS NULL
                                                  AND id_записи != @recordId";

                    using var command = new MySqlCommand(checkQuery, connection);
                    command.Parameters.AddWithValue("@employeeId", CurrentRecord.EmployeeId);
                    command.Parameters.AddWithValue("@recordId", CurrentRecord.Id);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0 && !_isEditMode)
                    {
                        MessageBox.Show("У сотрудника уже есть текущая должность! Закройте предыдущую должность перед добавлением новой.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка проверки данных: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
