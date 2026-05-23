using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace HrDepartment
{
    public partial class EmployeeDataWindow : Window
    {
        private ObservableCollection<Employee> _employees;
        public Employee NewEmployee { get; set; } = new Employee();
        public ObservableCollection<Department> Departments { get; set; } = new ObservableCollection<Department>();
        public ObservableCollection<Position> Positions { get; set; } = new ObservableCollection<Position>();

        public EmployeeDataWindow(ObservableCollection<Employee> employees)
        {
            InitializeComponent();
            _employees = employees;
            NewEmployee = new Employee();
            DataContext = this;
            LoadDepartmentsAndPositions();
        }

        private void LoadDepartmentsAndPositions()
        {
            string connectionString = "Server=localhost;Database=fin_project;Uid=root;Pwd=PowerSlav5577;";

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
        private void SaveEmployee(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            try
            {
                using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=fin_project;Uid=root;Pwd=PowerSlav5577;"))
                {
                    connection.Open();

                    // 1. Проверка уникальности должности в отделе
                    string checkQuery = @"SELECT COUNT(*) 
                                FROM сотрудник 
                                WHERE id_подразделения = @DepartmentId 
                                AND id_должности = @PositionId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", NewEmployee.DepartmentId);
                    checkCmd.Parameters.AddWithValue("@PositionId", NewEmployee.PositionId);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        MessageBox.Show("В этом отделе уже есть сотрудник с такой должностью!");
                        return;
                    }

                    // 2. Исправленный INSERT-запрос (добавлен параметр Office)
                    string insertQuery = @"INSERT INTO сотрудник 
                                (ФИО, Номер_телефона, Номер_телефона_раб, Корпоративный_email, Кабинет, id_подразделения, id_должности, Дата_рождения, В_отпуске)
                                VALUES (@FullName, @Phone, @WorkPhone, @Email, @Office, @DepartmentId, @PositionId, @BirthDate, FALSE)";

                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection);

                    // 3. Корректное заполнение параметров
                    insertCmd.Parameters.AddWithValue("@FullName", NewEmployee.FullName);
                    insertCmd.Parameters.AddWithValue("@Phone", NewEmployee.PersonalPhone);
                    insertCmd.Parameters.AddWithValue("@WorkPhone", NewEmployee.WorkPhone ?? (object)DBNull.Value); // Обработка NULL
                    insertCmd.Parameters.AddWithValue("@Email", NewEmployee.Email);
                    insertCmd.Parameters.AddWithValue("@Office", NewEmployee.Office ?? "Не указано"); // Значение по умолчанию
                    insertCmd.Parameters.AddWithValue("@DepartmentId", NewEmployee.DepartmentId);
                    insertCmd.Parameters.AddWithValue("@PositionId", NewEmployee.PositionId);
                    insertCmd.Parameters.AddWithValue("@BirthDate", NewEmployee.BirthDate.ToString("yyyy-MM-dd"));
                    insertCmd.Parameters.AddWithValue("@Address", NewEmployee.Address ?? (object)DBNull.Value);
                    //insertCmd.Parameters.AddWithValue("@WorkPhone", NewEmployee.WorkPhone ?? (object)DBNull.Value);

                    insertCmd.ExecuteNonQuery();
                }

                MessageBox.Show(
                            $"✅ Сотрудник успешно добавлен!\n\n" +
                            $"ФИО: {NewEmployee.FullName}\n" +
                            $"Email: {NewEmployee.Email}\n" +
                            $"Телефон: {NewEmployee.PersonalPhone}\n" +
                            $"Отдел: {NewEmployee.DepartmentId}",
                            "Добавление завершено",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                            );


                _employees.Add(NewEmployee);
                DialogResult = true;
                Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}\nКод ошибки: {ex.Number}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(NewEmployee.FullName) ||
                string.IsNullOrWhiteSpace(NewEmployee.PersonalPhone) ||
                string.IsNullOrWhiteSpace(NewEmployee.Email) ||
                NewEmployee.DepartmentId <= 0 ||
                NewEmployee.PositionId <= 0)
            {
                MessageBox.Show("Все поля обязательны для заполнения!");
                return false;
            }

            if (NewEmployee.DepartmentId <= 0)
            {
                MessageBox.Show("ID подразделения должен быть положительным числом!");
                return false;
            }
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(NewEmployee.FullName))
            {
                MessageBox.Show("ФИО не может быть пустым!");
                return false;
            }
            DateTime minDate = new DateTime(1971, 1, 1);

            if (!DateTime.TryParseExact(
                NewEmployee.BirthDateString,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime birthDate))
            {
                MessageBox.Show("Некорректный формат даты! Используйте ГГГГ-ММ-ДД");
                return false;
            }

            if (birthDate < minDate || birthDate > DateTime.Now)
            {
                MessageBox.Show($"Дата рождения должна быть между {minDate:yyyy-MM-dd} и {DateTime.Now:yyyy-MM-dd}");
                return false;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(NewEmployee.FullName, @"\d"))
            {
                MessageBox.Show("ФИО не должно содержать цифр!");
                return false;
            }

            // Проверка личного телефона (начинается с 8, 11 цифр)
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    NewEmployee.PersonalPhone,
                    @"^8\d{10}$"))
            {
                MessageBox.Show("Личный телефон должен начинаться с 8 и содержать 11 цифр! Пример: 89995576505");
                return false;
            }

            // Проверка рабочего телефона (необязательное поле, но если заполнено — должно быть корректным)
            if (!string.IsNullOrEmpty(NewEmployee.WorkPhone) &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    NewEmployee.WorkPhone,
                    @"^8\d{10}$"))
            {
                MessageBox.Show("Рабочий телефон должен начинаться с 8 и содержать 11 цифр! Пример: 89991234567");
                return false;
            }

            // Проверка email (с обрезанием пробелов)
            if (string.IsNullOrWhiteSpace(NewEmployee.Email) ||
                !System.Text.RegularExpressions.Regex.IsMatch(
                    NewEmployee.Email.Trim(),
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                MessageBox.Show("Некорректный email! Пример: user@example.com");
                return false;
            }

            NewEmployee.BirthDate = birthDate;
            return true;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

}
