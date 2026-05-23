using System.Windows;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;

namespace HrDepartment
{
    public partial class CareerHistoryWindow : Window
    {
        private Employee _employee;
        private string _connectionString = "Server=localhost;Database=fin_project;Uid=root;Pwd=PowerSlav5577;";

        public ObservableCollection<CareerHistory> CareerHistory { get; set; }
        public string EmployeeName => _employee.FullName;

        public CareerHistoryWindow(Employee employee)
        {
            InitializeComponent();
            _employee = employee;
            CareerHistory = new ObservableCollection<CareerHistory>();
            DataContext = this;
            LoadCareerHistory();
        }

        private void LoadCareerHistory()
        {
            CareerHistory.Clear();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                    h.id_записи, h.id_сотрудника, h.id_отдела, h.id_должности,
                                    h.дата_начала, h.дата_окончания, h.зарплата, h.примечание,
                                    d.Название_подразделения AS DepartmentName,
                                    p.Название_должности AS PositionName
                                FROM история_карьеры h
                                LEFT JOIN структурное_подразделение d ON h.id_отдела = d.id_department
                                LEFT JOIN должность p ON h.id_должности = p.id_position
                                WHERE h.id_сотрудника = @employeeId
                                ORDER BY h.дата_начала DESC";

                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@employeeId", _employee.Id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int departmentNameOrdinal = reader.GetOrdinal("DepartmentName");
                            int positionNameOrdinal = reader.GetOrdinal("PositionName");
                            int noteOrdinal = reader.GetOrdinal("примечание");

                            CareerHistory.Add(new CareerHistory
                            {
                                Id = reader.GetInt32("id_записи"),
                                EmployeeId = reader.GetInt32("id_сотрудника"),
                                DepartmentId = reader.GetInt32("id_отдела"),
                                PositionId = reader.GetInt32("id_должности"),
                                DepartmentName = reader.IsDBNull(departmentNameOrdinal) ? "Не указан" : reader.GetString(departmentNameOrdinal),
                                PositionName = reader.IsDBNull(positionNameOrdinal) ? "Не указан" : reader.GetString(positionNameOrdinal),
                                StartDate = reader.GetDateTime("дата_начала"),
                                EndDate = reader.IsDBNull(reader.GetOrdinal("дата_окончания")) ? null : reader.GetDateTime("дата_окончания"),
                                Salary = reader.IsDBNull(reader.GetOrdinal("зарплата")) ? 0 : reader.GetDecimal("зарплата"),
                                Note = reader.IsDBNull(noteOrdinal) ? "" : reader.GetString(noteOrdinal)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории карьеры: {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}