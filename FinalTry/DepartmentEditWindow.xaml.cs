using System;
using System.ComponentModel;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;

namespace HrDepartment
{
    public partial class DepartmentEditWindow : Window, INotifyPropertyChanged
    {
        private readonly string _connectionString = DbConfig.ConnectionString;
        private readonly bool _isEditMode;

        private string _departmentName = "";
        private int? _employeeCount;
        private string _description = "";
        private int? _departmentId;

        public string DepartmentName
        {
            get => _departmentName;
            set { _departmentName = value; OnPropertyChanged(nameof(DepartmentName)); }
        }

        public int? EmployeeCount
        {
            get => _employeeCount;
            set { _employeeCount = value; OnPropertyChanged(nameof(EmployeeCount)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string WindowTitle => _isEditMode ? "Редактирование отдела" : "Добавление отдела";

        public DepartmentEditWindow(DataRowView? row = null)
        {
            InitializeComponent();

            if (row is null)
            {
                _isEditMode = false;
                EmployeeCount = 0;
            }
            else
            {
                _isEditMode = true;
                _departmentId = Convert.ToInt32(row["id_department"]);
                DepartmentName = row["Название_подразделения"].ToString() ?? "";
                EmployeeCount = row["К-во_сотрудников"] == DBNull.Value ? 0 : Convert.ToInt32(row["К-во_сотрудников"]);
                Description = row["описание"] == DBNull.Value ? "" : row["описание"].ToString() ?? "";
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
                    const string updateQuery = @"UPDATE структурное_подразделение 
                                                SET Название_подразделения = @name,
                                                    `К-во_сотрудников` = @count,
                                                    описание = @description
                                                WHERE id_department = @id";

                    using var command = new MySqlCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@name", DepartmentName);
                    command.Parameters.AddWithValue("@count", EmployeeCount ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@description", Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@id", _departmentId);
                    command.ExecuteNonQuery();
                }
                else
                {
                    const string insertQuery = @"INSERT INTO структурное_подразделение 
                                                (Название_подразделения, `К-во_сотрудников`, описание)
                                                VALUES (@name, @count, @description)";

                    using var command = new MySqlCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@name", DepartmentName);
                    command.Parameters.AddWithValue("@count", EmployeeCount ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@description", Description ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(DepartmentName))
            {
                MessageBox.Show("Введите название отдела!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DepartmentNameTextBox.Focus();
                return false;
            }

            if (EmployeeCount.HasValue && EmployeeCount < 0)
            {
                MessageBox.Show("Количество сотрудников не может быть отрицательным!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmployeeCountTextBox.Focus();
                return false;
            }

            return true;
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
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
