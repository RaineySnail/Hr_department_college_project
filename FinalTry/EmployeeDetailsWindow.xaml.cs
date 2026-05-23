using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace HrDepartment
{
    public partial class EmployeeDetailsWindow : Window
    {
        private readonly User _currentUser;
        private readonly Employee _employee;

        public EmployeeDetailsWindow(Employee employee, User currentUser)
        {
            InitializeComponent();
            _employee = employee;
            _currentUser = currentUser;
            DataContext = employee;
            ApplyPermissions();
            UpdateTablesEmptyStates();
        }



        private void UpdateTablesEmptyStates()
        {
            if (CareerHistoryEmptyState != null)
                CareerHistoryEmptyState.Visibility = _employee.CareerHistory == null || _employee.CareerHistory.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (EventsEmptyState != null)
                EventsEmptyState.Visibility = _employee.Events == null || _employee.Events.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void ApplyPermissions()
        {
            bool canEdit = _currentUser.IsAdministrator || (_currentUser.IsSelfServiceUser && _currentUser.EmployeeId == _employee.Id);
            EditButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            SaveButton.Visibility = Visibility.Collapsed;
            VacationButton.Visibility = _currentUser.CanManageVacations ? Visibility.Visible : Visibility.Collapsed;
            CareerMoveButton.Visibility = _currentUser.CanManageCareer ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child is T t)
                        yield return t;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
            }
        }

        private void Vacation_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentUser.CanManageVacations)
            {
                MessageBox.Show("Оформление отпуска доступно только администратору.");
                return;
            }

            var window = new VacationRequestWindow(_employee)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                var service = new EventService(DbConfig.ConnectionString);
                service.RecalculateVacationFlagForEmployee(_employee.Id);
                _employee.LoadEvents(DbConfig.ConnectionString);
                _employee.IsOnVacation = true;
                UpdateTablesEmptyStates();
            }
        }

        private void SetEditMode(bool isEdit)
        {
            foreach (var child in FindVisualChildren<TextBox>(this))
            {
                child.IsReadOnly = !isEdit;
            }
        }

        private void CareerMove_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentUser.CanManageCareer)
            {
                MessageBox.Show("Кадровые перемещения доступны только администратору.");
                return;
            }

            var moveWindow = new CareerMoveWindow(_employee)
            {
                Owner = this
            };

            if (moveWindow.ShowDialog() == true)
            {
                _employee.LoadCareerHistory(DbConfig.ConnectionString);
                UpdateTablesEmptyStates();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            bool canEdit = _currentUser.IsAdministrator || (_currentUser.IsSelfServiceUser && _currentUser.EmployeeId == _employee.Id);
            if (!canEdit)
            {
                MessageBox.Show("Недостаточно прав для редактирования.");
                return;
            }

            SetEditMode(true);
            SaveButton.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var connection = new MySqlConnection(DbConfig.ConnectionString);
                connection.Open();

                string query = _currentUser.IsAdministrator
                    ? @"UPDATE сотрудник SET
                        ФИО = @name,
                        Адрес = @address,
                        Номер_телефона = @phone,
                        Корпоративный_email = @email,
                        Паспортные_данные = @passport,
                        СНИЛС = @snils,
                        ИНН = @inn,
                        Образование = @education,
                        Примечание = @note
                        WHERE id_employee = @id"
                    : @"UPDATE сотрудник SET
                        ФИО = @name,
                        Адрес = @address,
                        Номер_телефона = @phone,
                        Корпоративный_email = @email,
                        Паспортные_данные = @passport,
                        СНИЛС = @snils,
                        ИНН = @inn,
                        Образование = @education
                        WHERE id_employee = @id";

                using var cmd = new MySqlCommand(query, connection);

                cmd.Parameters.AddWithValue("@name", _employee.FullName);
                cmd.Parameters.AddWithValue("@address", _employee.Address);
                cmd.Parameters.AddWithValue("@phone", _employee.PersonalPhone);
                cmd.Parameters.AddWithValue("@email", _employee.Email);
                cmd.Parameters.AddWithValue("@passport", _employee.PassportData);
                cmd.Parameters.AddWithValue("@snils", _employee.SNILS);
                cmd.Parameters.AddWithValue("@inn", _employee.INN);
                cmd.Parameters.AddWithValue("@education", _employee.Education);
                cmd.Parameters.AddWithValue("@note", _employee.Note);
                cmd.Parameters.AddWithValue("@id", _employee.Id);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Данные сохранены", "Успех");

                SetEditMode(false);
                SaveButton.Visibility = Visibility.Collapsed;
                UpdateTablesEmptyStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
