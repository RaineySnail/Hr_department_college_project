using MySql.Data.MySqlClient; 
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Controls;
using System.Linq;

//using System.Text;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using System.Collections.Specialized;

namespace HrDepartment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Employee> Employees { get; set; } = new ObservableCollection<Employee>();

        private ICollectionView? _employeesView;

        private ObservableCollection<string> _departmentFilterItems = new ObservableCollection<string>();

        public User CurrentUser { get; set; }

        private string _connectionString = DbConfig.ConnectionString;// Строка подключения к MySQL

        public MainWindow(User user)
        {
            InitializeComponent();
            CurrentUser = user;
            DataContext = this;

            // Подготовка view для поиска/фильтра
            _employeesView = CollectionViewSource.GetDefaultView(Employees);
            _employeesView.Filter = EmployeesFilter;

            SetButtonVisibility("ManageDepartmentsButton", CurrentUser.CanManageDepartments);
            SetupUIForRole();

            // Авто-пересчёт статуса "В отпуске" при запуске
            try
            {
                var es = new EventService(_connectionString);
                es.RecalculateAllVacationFlags();
            }
            catch
            {
                // Если вдруг БД временно недоступна, не валим приложение — просто пропускаем.
            }

            LoadAllEmployees(); // Загружаем данные при старте

            EmployeesList.SelectionChanged += EmployeesList_SelectionChanged;
            Employees.CollectionChanged += Employees_CollectionChanged;

            // Первичное состояние кнопок и статуса
            UpdateSelectionStatus();
            UpdateEmployeesCount();
            UpdateEmptyState();
        }



        private void Employees_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateEmployeesCount();
            UpdateEmptyState();
        }

        private void EmployeesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmployeesList.SelectedItem is Employee selectedEmployee)
            {
                // Загружаем историю карьеры для выбранного сотрудника
                selectedEmployee.LoadCareerHistory(_connectionString);
            }

            UpdateSelectionStatus();
        }

                private void UpdateSelectionStatus()
        {
            bool selected = EmployeesList.SelectedItem != null;

            bool canEditSelected =
                selected &&
                (
                    CurrentUser.IsAdministrator ||
                    (
                        CurrentUser.IsSelfServiceUser &&
                        EmployeesList.SelectedItem is Employee selectedEmpForEdit &&
                        CurrentUser.EmployeeId == selectedEmpForEdit.Id
                    )
                );

            EditEmployeeButton.IsEnabled = canEditSelected;
            DeleteEmployeeButton.IsEnabled = selected && CurrentUser.CanDeleteEmployees;
            VacationButton.IsEnabled = selected && CurrentUser.CanManageVacations;
            CareerHistoryButton.IsEnabled = selected;

            if (EmployeesList.SelectedItem is Employee selectedEmp)
            {
                SelectedEmployeeText.Text = $"Выбран: {selectedEmp.FullName}";
                if (SelectedEmployeeNameText != null)
                    SelectedEmployeeNameText.Text = selectedEmp.FullName;
                if (SelectedEmployeeRoleText != null)
                    SelectedEmployeeRoleText.Text = $"{selectedEmp.PositionName} · {selectedEmp.DepartmentName}";
            }
            else
            {
                SelectedEmployeeText.Text = "Выбран: —";
                if (SelectedEmployeeNameText != null)
                    SelectedEmployeeNameText.Text = "—";
                if (SelectedEmployeeRoleText != null)
                    SelectedEmployeeRoleText.Text = "Выберите запись в списке для быстрых действий";
            }
        }

        private void UpdateEmptyState()
        {
            if (EmptyStateBorder == null)
                return;

            int count = _employeesView?.Cast<object>().Count() ?? Employees.Count;
            EmptyStateBorder.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateEmployeesCount()
        {
            int count = _employeesView?.Cast<object>().Count() ?? Employees.Count;
            int vacationCount = (_employeesView?.Cast<Employee>().Count(e => e.IsOnVacation)).GetValueOrDefault();

            if (EmployeesCountText != null)
                EmployeesCountText.Text = $"Сотрудников: {count}";

            if (MetricEmployeesCountText != null)
                MetricEmployeesCountText.Text = count.ToString();

            if (MetricVacationCountText != null)
                MetricVacationCountText.Text = vacationCount.ToString();
        }

        private bool EmployeesFilter(object obj)
        {
            if (obj is not Employee emp)
                return false;

            string searchText = (EmployeeSearchBox?.Text ?? "").Trim();
            string selectedDepartment = DepartmentFilterComboBox?.SelectedItem as string ?? "Все отделы";
            bool vacationOnly = VacationOnlyCheckBox?.IsChecked == true;

            bool matchesSearch =
                string.IsNullOrWhiteSpace(searchText)
                || (emp.FullName ?? "").IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                || (emp.DepartmentName ?? "").IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                || (emp.PositionName ?? "").IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

            bool matchesDepartment =
                selectedDepartment == "Все отделы"
                || string.Equals(emp.DepartmentName ?? "", selectedDepartment, StringComparison.OrdinalIgnoreCase);

            bool matchesVacation = !vacationOnly || emp.IsOnVacation;

            return matchesSearch && matchesDepartment && matchesVacation;
        }

        private void EmployeeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _employeesView?.Refresh();
            UpdateEmployeesCount();
            UpdateEmptyState();
        }

        private void DepartmentFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_employeesView == null)
                return;

            _employeesView.Refresh();
            UpdateEmployeesCount();
            UpdateEmptyState();
        }

        private void VacationOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _employeesView?.Refresh();
            UpdateEmployeesCount();
            UpdateEmptyState();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeSearchBox != null)
                EmployeeSearchBox.Text = string.Empty;

            if (DepartmentFilterComboBox != null)
                DepartmentFilterComboBox.SelectedIndex = 0;

            if (VacationOnlyCheckBox != null)
                VacationOnlyCheckBox.IsChecked = false;

            _employeesView?.Refresh();
            UpdateEmployeesCount();
            UpdateEmptyState();
        }

        private void LoadDepartmentFilterItems()
        {
            _departmentFilterItems.Clear();
            _departmentFilterItems.Add("Все отделы");

            foreach (string department in Employees
                         .Select(e => e.DepartmentName)
                         .Where(name => !string.IsNullOrWhiteSpace(name))
                         .Distinct()
                         .OrderBy(name => name))
            {
                _departmentFilterItems.Add(department);
            }

            DepartmentFilterComboBox.ItemsSource = _departmentFilterItems;

            if (DepartmentFilterComboBox.SelectedIndex < 0)
                DepartmentFilterComboBox.SelectedIndex = 0;
        }

        //public MainWindow() : this(new User { RoleId = 3, RoleName = "Наблюдатель" }) { }

        private void SetupUIForRole()
        {
            // Показываем информацию о пользователе
            Title = $"HR Department - {CurrentUser.FullName} ({CurrentUser.RoleName})";

            // Плашка в шапке (если есть)
            if (FindName("UserInfoText") is TextBlock tb)
                tb.Text = $"Пользователь: {CurrentUser.FullName}\nРоль: {CurrentUser.RoleName}";


            // Настраиваем видимость кнопок в зависимости от прав
            SetButtonVisibility("EditEmployeeButton", CurrentUser.IsAdministrator || CurrentUser.CanEditSelfOnly);
            SetButtonVisibility("DeleteEmployeeButton", CurrentUser.CanDeleteEmployees);
            SetButtonVisibility("AddEmployeeButton", CurrentUser.CanEditEmployees);
            SetButtonVisibility("CareerHistoryButton", true);
            SetButtonVisibility("VacationButton", CurrentUser.CanManageVacations);
            SetButtonVisibility("EventsJournalButton", CurrentUser.CanViewEventsJournal);
            SetButtonVisibility("EventTypesButton", CurrentUser.CanViewEventTypes);
            SetButtonVisibility("BirthdayReportButton", CurrentUser.CanViewReports);

            SetButtonVisibility("ManageUsersButton", CurrentUser.CanManageUsers);
            SetButtonVisibility("ReportButton", CurrentUser.CanViewReports);
            SetButtonVisibility("ManageDepartmentsButton", CurrentUser.CanManageDepartments);
        }

        public class BooleanToStatusConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (bool)value ? "Активен" : "Заблокирован";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Binding.DoNothing;
            }
        }
        private void SetButtonVisibility(string buttonName, bool isVisible)
        {
            var button = FindName(buttonName) as Button;
            if (button != null)
            {
                button.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void ShowPositions_Click(object sender, RoutedEventArgs e)
        {
            LoadPositionsFromView();
        }

        private void LoadAllEmployees()
        {
            Employees.Clear();

            try
            {
                var eventService = new EventService(_connectionString);
                eventService.RecalculateAllVacationFlags();

                using MySqlConnection connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"SELECT 
                            e.id_employee,
                            e.ФИО,
                            e.Адрес,
                            e.Дата_рождения,
                            e.Номер_телефона,
                            e.Номер_телефона_раб,
                            e.Кабинет,
                            e.id_подразделения,
                            e.id_должности,
                            e.Корпоративный_email,
                            e.В_отпуске,
                            e.Пол,
                            e.Место_рождения,
                            e.Паспортные_данные,
                            e.СНИЛС,
                            e.ИНН,
                            e.Семейное_положение,
                            e.Гражданство,
                            e.Образование,
                            e.Дата_приема,
                            e.Примечание,
                            d.Название_подразделения AS DepartmentName,
                            p.Название_должности AS PositionName
                         FROM сотрудник e
                         LEFT JOIN структурное_подразделение d ON e.id_подразделения = d.id_department
                         LEFT JOIN должность p ON e.id_должности = p.id_position
                         WHERE (@selfOnly = 0 OR e.id_employee = @employeeId)
                         ORDER BY e.ФИО;";

                using MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@selfOnly", CurrentUser.IsSelfServiceUser ? 1 : 0);
                command.Parameters.AddWithValue("@employeeId", CurrentUser.EmployeeId ?? 0);
                using MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var employee = new Employee
                    {
                        Id = Convert.ToInt32(reader["id_employee"]),
                        FullName = reader.GetStringSafe("ФИО"),
                        Address = reader.GetStringSafe("Адрес"),
                        BirthDate = reader.IsDBNull(reader.GetOrdinal("Дата_рождения"))
                            ? default
                            : reader.GetDateTime("Дата_рождения"),
                        PersonalPhone = reader.GetStringSafe("Номер_телефона"),
                        WorkPhone = reader.GetStringSafe("Номер_телефона_раб"),
                        Office = reader.GetStringSafe("Кабинет"),
                        DepartmentId = reader.GetInt32Safe("id_подразделения"),
                        PositionId = reader.GetInt32Safe("id_должности"),
                        Email = reader.GetStringSafe("Корпоративный_email"),
                        IsOnVacation = !reader.IsDBNull(reader.GetOrdinal("В_отпуске")) && reader.GetBoolean("В_отпуске"),
                        DepartmentName = reader.GetStringSafe("DepartmentName"),
                        PositionName = reader.GetStringSafe("PositionName"),

                        Gender = reader.GetStringSafe("Пол"),
                        PlaceOfBirth = reader.GetStringSafe("Место_рождения"),
                        PassportData = reader.GetStringSafe("Паспортные_данные"),
                        SNILS = reader.GetStringSafe("СНИЛС"),
                        INN = reader.GetStringSafe("ИНН"),
                        MaritalStatus = reader.GetStringSafe("Семейное_положение"),
                        Citizenship = reader.GetStringSafe("Гражданство"),
                        Education = reader.GetStringSafe("Образование"),
                        EmploymentDate = reader.IsDBNull(reader.GetOrdinal("Дата_приема"))
                            ? null
                            : reader.GetDateTime("Дата_приема"),
                        Note = reader.GetStringSafe("Примечание"),
                        CurrentSalary = 0
                    };

                    Employees.Add(employee);
                }

                LoadDepartmentFilterItems();
                _employeesView?.Refresh();
                UpdateEmployeesCount();
                UpdateEmptyState();
                UpdateSelectionStatus();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EmployeesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EmployeesList.SelectedItem is not Employee selectedEmployee)
                return;

            try
            {
                var eventService = new EventService(_connectionString);
                eventService.RecalculateVacationFlagForEmployee(selectedEmployee.Id);

                selectedEmployee.LoadCareerHistory(_connectionString);
                selectedEmployee.LoadEvents(_connectionString);

                var detailsWindow = new EmployeeDetailsWindow(selectedEmployee, CurrentUser)
                {
                    Owner = this
                };

                detailsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть карточку сотрудника.\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {

            if (!CurrentUser.CanDeleteEmployees)
            {
                MessageBox.Show("Недостаточно прав для удаления сотрудников");
                return;
            }

            try
            {
                var selectedEmployee = EmployeesList.SelectedItem as Employee;
                if (selectedEmployee == null)
                {
                    MessageBox.Show("Выберите сотрудника из списка!");
                    return;
                }

                if (selectedEmployee.IsOnVacation)
                {
                    MessageBox.Show("Нельзя удалить сотрудника, который в отпуске!");
                    return;
                }

                var result = MessageBox.Show(
                    $"Удалить сотрудника {selectedEmployee.FullName}?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes) return;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                
                    var cmd = new MySqlCommand(
                        "DELETE FROM сотрудник WHERE id_employee = @id", 
                        connection
                    );

                   
                    Console.WriteLine($"Deleting employee with ID: {selectedEmployee.Id}");

                    cmd.Parameters.AddWithValue("@id", selectedEmployee.Id);
                    int affectedRows = cmd.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        MessageBox.Show("Сотрудник не найден в базе!");
                        return;
                    }
                }

                Employees.Remove(selectedEmployee);
                MessageBox.Show("Сотрудник удалён!");
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка MySQL: {ex.Message}\nКод ошибки: {ex.Number}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            // На всякий случай пересчитываем "В отпуске" перед обновлением списка
            try
            {
                var es = new EventService(_connectionString);
                es.RecalculateAllVacationFlags();
            }
            catch { }

            LoadAllEmployees();
            // Не спамим MessageBox — всё видно по списку/счётчику
        }

        private void OpenEventsJournal_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewEventsJournal)
            {
                MessageBox.Show("Недостаточно прав для просмотра журнала событий.");
                return;
            }

            var w = new EventsJournalWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void OpenEventTypes_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewEventTypes)
            {
                MessageBox.Show("Недостаточно прав для просмотра типов событий.");
                return;
            }

            var w = new EventTypesWindow(CurrentUser.CanManageEventTypes);
            w.Owner = this;
            w.ShowDialog();
        }

        private void OpenEventOnDateReport_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewReports)
            {
                MessageBox.Show("Недостаточно прав для просмотра отчётов.");
                return;
            }

            var w = new EventOnDateReportWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void OpenDepartmentCountReport_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewReports)
            {
                MessageBox.Show("Недостаточно прав для просмотра отчётов.");
                return;
            }

            var w = new DepartmentCountReportWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void OpenBirthdayReport_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewReports)
            {
                MessageBox.Show("Недостаточно прав для просмотра отчётов.");
                return;
            }

            var w = new BirthdayReportWindow();
            w.Owner = this;
            w.ShowDialog();
        }


        private void CreateVacationEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageVacations)
            {
                MessageBox.Show("Оформлять отпуска может только администратор кадровой системы.");
                return;
            }

            var selectedEmployee = EmployeesList.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника из списка!");
                return;
            }

            var w = new VacationRequestWindow(selectedEmployee)
            {
                Owner = this
            };

            if (w.ShowDialog() == true)
            {
                LoadAllEmployees();
            }
        }


        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesList.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника из списка!");
                return;
            }

            bool canEdit = CurrentUser.IsAdministrator || (CurrentUser.IsSelfServiceUser && CurrentUser.EmployeeId == selectedEmployee.Id);
            if (!canEdit)
            {
                MessageBox.Show("Недостаточно прав для редактирования этого сотрудника");
                return;
            }

            // Создаем копию для редактирования
            var employeeCopy = new Employee
            {
                Id = selectedEmployee.Id,
                FullName = selectedEmployee.FullName,
                PersonalPhone = selectedEmployee.PersonalPhone,
                WorkPhone = selectedEmployee.WorkPhone,
                Office = selectedEmployee.Office,
                DepartmentId = selectedEmployee.DepartmentId,
                PositionId = selectedEmployee.PositionId,
                Email = selectedEmployee.Email,
                BirthDate = selectedEmployee.BirthDate,
                Address = selectedEmployee.Address,
                IsOnVacation = selectedEmployee.IsOnVacation,
                DepartmentName = selectedEmployee.DepartmentName,
                PositionName = selectedEmployee.PositionName
            };

            // Открываем окно редактирования
            var editWindow = new EditEmployeeWindow(employeeCopy, CurrentUser.IsSelfServiceUser);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    // Обновляем данные в БД
                    using (var connection = new MySqlConnection(_connectionString))
                    {
                        connection.Open();
                        string updateSql = CurrentUser.IsAdministrator
                            ? @"UPDATE сотрудник 
                        SET ФИО = @FullName, 
                        Номер_телефона = @Phone,
                        Номер_телефона_раб = @WorkPhone,
                        Кабинет = @Office,
                        id_подразделения = @DepartmentId,
                        id_должности = @PositionId,
                        Корпоративный_email = @Email,
                        Адрес = @Address
                        WHERE id_employee = @Id"
                            : @"UPDATE сотрудник 
                        SET ФИО = @FullName,
                        Номер_телефона = @Phone,
                        Корпоративный_email = @Email,
                        Адрес = @Address,
                        Дата_рождения = @BirthDate
                        WHERE id_employee = @Id";

                        var cmd = new MySqlCommand(updateSql, connection);

                        cmd.Parameters.AddWithValue("@FullName", employeeCopy.FullName);
                        cmd.Parameters.AddWithValue("@Phone", employeeCopy.PersonalPhone);
                        cmd.Parameters.AddWithValue("@Email", employeeCopy.Email);
                        cmd.Parameters.AddWithValue("@Address", employeeCopy.Address);
                        cmd.Parameters.AddWithValue("@BirthDate", employeeCopy.BirthDate == default ? DBNull.Value : employeeCopy.BirthDate);
                        cmd.Parameters.AddWithValue("@WorkPhone", employeeCopy.WorkPhone);
                        cmd.Parameters.AddWithValue("@Office", employeeCopy.Office);
                        cmd.Parameters.AddWithValue("@DepartmentId", employeeCopy.DepartmentId);
                        cmd.Parameters.AddWithValue("@PositionId", employeeCopy.PositionId);
                        cmd.Parameters.AddWithValue("@Id", employeeCopy.Id);

                        cmd.ExecuteNonQuery();
                    }

                    // Загружаем обновлённые данные названий отделов и должностей
                    var updatedEmployee = GetUpdatedEmployee(employeeCopy.Id);

                    // Обновляем данные в основном списке
                    selectedEmployee.FullName = employeeCopy.FullName;
                    selectedEmployee.PersonalPhone = employeeCopy.PersonalPhone;
                    selectedEmployee.WorkPhone = employeeCopy.WorkPhone;
                    selectedEmployee.Office = employeeCopy.Office;
                    selectedEmployee.DepartmentId = employeeCopy.DepartmentId;
                    selectedEmployee.PositionId = employeeCopy.PositionId;
                    selectedEmployee.Email = employeeCopy.Email;
                    selectedEmployee.Address = employeeCopy.Address;

                    // Обновляем названия отделов и должностей
                    if (updatedEmployee != null)
                    {
                        selectedEmployee.DepartmentName = updatedEmployee.DepartmentName;
                        selectedEmployee.PositionName = updatedEmployee.PositionName;
                    }

                    MessageBox.Show("Данные сотрудника обновлены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        // Метод для получения обновлённых данных сотрудника
        private Employee? GetUpdatedEmployee(long employeeId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                d.Название_подразделения AS DepartmentName,
                                p.Название_должности AS PositionName
                            FROM сотрудник e
                            LEFT JOIN структурное_подразделение d ON e.id_подразделения = d.id_department
                            LEFT JOIN должность p ON e.id_должности = p.id_position
                            WHERE e.id_employee = @id;";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", employeeId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Employee
                            {
                                DepartmentName = reader["DepartmentName"] == DBNull.Value ? "Не указан" : reader.GetString("DepartmentName"),
                                PositionName = reader["PositionName"] == DBNull.Value ? "Не указан" : reader.GetString("PositionName")
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении обновлённых данных: {ex.Message}");
            }
            return null;
        }
        private void ToggleVacationStatus_Click(object sender, RoutedEventArgs e)
        {
            var selectedEmployee = EmployeesList.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника из списка!");
                return;
            }

            string message = selectedEmployee.IsOnVacation ?
                "Снять статус 'В отпуске'?" :
                "Установить статус 'В отпуске'?";

            var result = MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Обновляем статус в базе данных
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new MySqlCommand(
                        "UPDATE сотрудник SET В_отпуске = @isOnVacation WHERE id_employee = @id",
                        connection
                    );
                    cmd.Parameters.AddWithValue("@isOnVacation", !selectedEmployee.IsOnVacation);
                    cmd.Parameters.AddWithValue("@id", selectedEmployee.Id);
                    cmd.ExecuteNonQuery();
                }

                // Обновляем статус в приложении
                selectedEmployee.IsOnVacation = !selectedEmployee.IsOnVacation;

                MessageBox.Show(selectedEmployee.IsOnVacation ?
                    "Сотрудник теперь в отпуске!" :
                    "Сотрудник вышел из отпуска!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanViewReports)
            {
                MessageBox.Show("Недостаточно прав для просмотра отчётов");
                return;
            }

            if (Employees.Count == 0)
            {
                MessageBox.Show("Нет данных для формирования отчёта!");
                return;
            }

            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "Отчет_по_сотрудникам",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv"
                };

                if (dlg.ShowDialog() != true) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("ID;ФИО;Отдел;Должность;Email;Телефон;Дата рождения;В отпуске");

                foreach (var e1 in Employees)
                {
                    sb.AppendLine($"{e1.Id};{e1.FullName};{e1.DepartmentName};{e1.PositionName};{e1.Email};{e1.PersonalPhone};{e1.BirthDate:dd.MM.yyyy};{(e1.IsOnVacation ? "Да" : "Нет")}");
                }

                System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show("Отчёт сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчёта: {ex.Message}");
            }
        }



        private void ManageDepartments_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.IsAdministrator)
            {
                MessageBox.Show("Недостаточно прав для управления отделами", "Ошибка прав доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var departmentsWindow = new DepartmentsWindow();
            departmentsWindow.ShowDialog();

            // Обновляем данные сотрудников, если были изменения в отделах
            LoadAllEmployees();
        }

        private void OpenEmployeeDataWindow(object sender, RoutedEventArgs e)
        {
            

            EmployeeDataWindow employeeDataWindow = new EmployeeDataWindow(Employees);
            employeeDataWindow.ShowDialog();
            LoadAllEmployees();
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            LoadAllEmployees();
        }

        private void ShowCareerHistory_Click(object sender, RoutedEventArgs e)
        {
           

            var selectedEmployee = EmployeesList.SelectedItem as Employee;
            if (selectedEmployee == null)
            {
                MessageBox.Show("Выберите сотрудника из списка!");
                return;
            }

            var careerWindow = new CareerHistoryWindow(selectedEmployee);
            careerWindow.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из текущего профиля и вернуться к окну входа?", "Смена пользователя", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void ManageUsers_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUser.CanManageUsers)
            {
                MessageBox.Show("Недостаточно прав для управления пользователями");
                return;
            }

            var usersWindow = new UsersManagementWindow(CurrentUser);
            usersWindow.ShowDialog();
        }
        private void LoadPositionsFromView()
        {
            Employees.Clear();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM курсовая_работа.сотрудник;";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Employees.Add(new Employee
                            {
                                FullName = reader.GetStringSafe("ФИО"),
                                //Должность = reader.GetStringSafe("Название_должности")
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
