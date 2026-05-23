using MySql.Data.MySqlClient;
using System.Globalization;
using System.Windows;

namespace HrDepartment
{
    public partial class CareerMoveWindow : Window
    {
        private readonly Employee _employee;

        public CareerMoveWindow(Employee employee)
        {
            InitializeComponent();
            _employee = employee;
            DataContext = employee;

            LoadDepartments();
            LoadPositions();

            StartDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadDepartments()
        {
            var items = new List<LookupItem>();

            using var connection = new MySqlConnection(DbConfig.ConnectionString);
            connection.Open();

            string query = @"SELECT id_department, Название_подразделения
                             FROM структурное_подразделение
                             ORDER BY Название_подразделения";

            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new LookupItem
                {
                    Id = reader.GetInt32("id_department"),
                    Name = reader.GetString("Название_подразделения")
                });
            }

            DepartmentComboBox.ItemsSource = items;
            DepartmentComboBox.SelectedValue = _employee.DepartmentId;
        }

        private void LoadPositions()
        {
            var items = new List<LookupItem>();

            using var connection = new MySqlConnection(DbConfig.ConnectionString);
            connection.Open();

            string query = @"SELECT id_position, Название_должности
                             FROM должность
                             ORDER BY Название_должности";

            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new LookupItem
                {
                    Id = reader.GetInt32("id_position"),
                    Name = reader.GetString("Название_должности")
                });
            }

            PositionComboBox.ItemsSource = items;
            PositionComboBox.SelectedValue = _employee.PositionId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DepartmentComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите подразделение.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PositionComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Выберите должность.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StartDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Укажите дату начала.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                decimal? salary = null;

                int departmentId = (int)DepartmentComboBox.SelectedValue;
                int positionId = (int)PositionComboBox.SelectedValue;
                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                string note = NoteTextBox.Text?.Trim() ?? "";

                using var connection = new MySqlConnection(DbConfig.ConnectionString);
                connection.Open();

                using var transaction = connection.BeginTransaction();

                string closeCurrentQuery = @"UPDATE история_карьеры
                                            SET дата_окончания = DATE_SUB(@startDate, INTERVAL 1 DAY)
                                            WHERE id_сотрудника = @employeeId
                                              AND дата_окончания IS NULL;";

                using (var closeCommand = new MySqlCommand(closeCurrentQuery, connection, transaction))
                {
                    closeCommand.Parameters.AddWithValue("@startDate", startDate);
                    closeCommand.Parameters.AddWithValue("@employeeId", _employee.Id);
                    closeCommand.ExecuteNonQuery();
                }

                string insertHistoryQuery = @"INSERT INTO история_карьеры
                                            (id_сотрудника, id_отдела, id_должности, дата_начала, дата_окончания, зарплата, примечание)
                                            VALUES
                                            (@employeeId, @departmentId, @positionId, @startDate, NULL, @salary, @note);";

                using (var insertCommand = new MySqlCommand(insertHistoryQuery, connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@employeeId", _employee.Id);
                    insertCommand.Parameters.AddWithValue("@departmentId", departmentId);
                    insertCommand.Parameters.AddWithValue("@positionId", positionId);
                    insertCommand.Parameters.AddWithValue("@startDate", startDate);
                    insertCommand.Parameters.AddWithValue("@salary", (object?)salary ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@note", note);
                    insertCommand.ExecuteNonQuery();
                }

                string updateEmployeeQuery = @"UPDATE сотрудник
                                              SET id_подразделения = @departmentId,
                                                  id_должности = @positionId,
                                                  Дата_приема = COALESCE(Дата_приема, @startDate)
                                              WHERE id_employee = @employeeId;";

                using (var updateCommand = new MySqlCommand(updateEmployeeQuery, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@departmentId", departmentId);
                    updateCommand.Parameters.AddWithValue("@positionId", positionId);
                    updateCommand.Parameters.AddWithValue("@startDate", startDate);
                    updateCommand.Parameters.AddWithValue("@employeeId", _employee.Id);
                    updateCommand.ExecuteNonQuery();
                }

                transaction.Commit();

                _employee.DepartmentId = departmentId;
                _employee.PositionId = positionId;
                _employee.CurrentSalary = 0;

                if (DepartmentComboBox.SelectedItem is LookupItem dep)
                    _employee.DepartmentName = dep.Name;

                if (PositionComboBox.SelectedItem is LookupItem pos)
                    _employee.PositionName = pos.Name;

                if (_employee.EmploymentDate == null)
                    _employee.EmploymentDate = startDate;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось выполнить кадровое перемещение.\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}