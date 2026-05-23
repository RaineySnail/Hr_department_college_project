using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Windows;

namespace HrDepartment
{
    public partial class EditEmployeeWindow : Window
    {
        public Employee SelectedEmployee { get; set; }
        public ObservableCollection<Department> Departments { get; set; } = new ObservableCollection<Department>();
        public ObservableCollection<Position> Positions { get; set; } = new ObservableCollection<Position>();
        private readonly bool _selfOnly;

        public EditEmployeeWindow(Employee employee, bool selfOnly = false)
        {
            InitializeComponent();
            SelectedEmployee = employee;
            _selfOnly = selfOnly;
            DataContext = this;
            LoadDepartmentsAndPositions();
            ApplyRoleRestrictions();
        }

        private void LoadDepartmentsAndPositions()
        {
            string connectionString = DbConfig.ConnectionString;

            // Загрузка отделов
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT id_department, Название_подразделения FROM структурное_подразделение;";
                MySqlCommand cmd = new MySqlCommand(query, connection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Departments.Add(new Department
                        {
                            DepartmentId = reader.GetInt32("id_department"),
                            DepartmentName = reader.GetString("Название_подразделения")
                        });
                    }
                }
            }
            // Загрузка должностей
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT id_position, Название_должности FROM должность;";
                MySqlCommand cmd = new MySqlCommand(query, connection);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Positions.Add(new Position
                        {
                            PositionId = reader.GetInt32("id_position"),
                            PositionName = reader.GetString("Название_должности")
                        });
                    }
                }
            }
        }


        private void ApplyRoleRestrictions()
        {
            if (!_selfOnly)
                return;

            Title = "Редактирование своих данных";
            DepartmentComboBox.IsEnabled = false;
            PositionComboBox.IsEnabled = false;
            OfficeTextBox.IsReadOnly = true;
            WorkPhoneTextBox.IsReadOnly = true;
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Сохранить изменения?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = true;
                Close();
            }

        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
