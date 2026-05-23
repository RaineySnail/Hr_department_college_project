using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace HrDepartment
{
    public partial class UsersManagementWindow : Window
    {
        private readonly string _connectionString = DbConfig.ConnectionString;
        private readonly User _currentUser;

        public ObservableCollection<User> Users { get; } = new();
        public ObservableCollection<UserRoleOption> Roles { get; } = new();

        public UsersManagementWindow(User currentUser)
        {
            InitializeComponent();
            DataContext = this;
            _currentUser = currentUser;

            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            Roles.Clear();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            const string query = @"
SELECT id_роли, название_роли
FROM роли
ORDER BY id_роли;";

            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Roles.Add(new UserRoleOption
                {
                    Id = reader.GetInt32("id_роли"),
                    Name = reader.GetStringSafe("название_роли")
                });
            }
        }

        private void LoadUsers()
        {
            Users.Clear();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = @"
SELECT u.id,
       u.username,
       u.id_роли,
       r.название_роли AS role_name,
       u.ФИО,
       u.активен,
       u.employee_id
FROM users u
LEFT JOIN роли r ON u.id_роли = r.id_роли
ORDER BY u.username;";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Users.Add(new User
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetStringSafe("username"),
                        RoleId = reader.GetInt32("id_роли"),
                        RoleName = reader.GetStringSafe("role_name"),
                        FullName = reader.GetStringSafe("ФИО"),
                        IsActive = reader.GetBoolean("активен"),
                        EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt64("employee_id")
                    });
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            int activeCount = Users.Count(user => user.IsActive);
            int blockedCount = Users.Count - activeCount;
            UsersSummaryText.Text = $"Всего пользователей: {Users.Count}  Активных: {activeCount}  Заблокированных: {blockedCount}";
        }

        private User? GetSelectedUser()
        {
            if (UsersList.SelectedItem is User user)
                return user;

            MessageBox.Show("Выберите пользователя из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UserEditWindow("Добавление пользователя", Roles)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true)
                    return;

                User user = dialog.ResultUser;

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = @"
INSERT INTO users (username, password, id_роли, ФИО, активен)
VALUES (@username, @password, @roleId, @fullName, @isActive);";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@password", user.Password);
                command.Parameters.AddWithValue("@roleId", user.RoleId);
                command.Parameters.AddWithValue("@fullName", user.FullName);
                command.Parameters.AddWithValue("@isActive", user.IsActive);
                command.ExecuteNonQuery();

                LoadUsers();
                MessageBox.Show("Пользователь успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Не удалось добавить пользователя: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось добавить пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            User? selectedUser = GetSelectedUser();
            if (selectedUser is null)
                return;

            try
            {
                var dialog = new UserEditWindow("Редактирование пользователя", Roles, selectedUser)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() != true)
                    return;

                User user = dialog.ResultUser;

                if (selectedUser.Id == _currentUser.Id)
                {
                    if (!user.IsActive)
                    {
                        MessageBox.Show("Нельзя деактивировать текущую учётную запись.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (user.RoleId != selectedUser.RoleId)
                    {
                        MessageBox.Show("Нельзя менять роль текущему пользователю из собственной сессии.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
UPDATE users
SET username = @username,
    id_роли = @roleId,
    ФИО = @fullName,
    активен = @isActive";

                if (!string.IsNullOrWhiteSpace(user.Password))
                    query += ", password = @password";

                query += " WHERE id = @id;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", selectedUser.Id);
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@roleId", user.RoleId);
                command.Parameters.AddWithValue("@fullName", user.FullName);
                command.Parameters.AddWithValue("@isActive", user.IsActive);

                if (!string.IsNullOrWhiteSpace(user.Password))
                    command.Parameters.AddWithValue("@password", user.Password);

                command.ExecuteNonQuery();

                LoadUsers();
                MessageBox.Show("Данные пользователя обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Не удалось обновить пользователя: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось обновить пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleUserActive_Click(object sender, RoutedEventArgs e)
        {
            User? selectedUser = GetSelectedUser();
            if (selectedUser is null)
                return;

            if (selectedUser.Id == _currentUser.Id)
            {
                MessageBox.Show("Нельзя заблокировать текущую учётную запись.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool newStatus = !selectedUser.IsActive;
            string actionText = newStatus ? "разблокировать" : "заблокировать";

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите {actionText} пользователя '{selectedUser.Username}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = "UPDATE users SET активен = @isActive WHERE id = @id;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", selectedUser.Id);
                command.Parameters.AddWithValue("@isActive", newStatus);
                command.ExecuteNonQuery();

                LoadUsers();
                MessageBox.Show(
                    newStatus ? "Пользователь разблокирован." : "Пользователь заблокирован.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Не удалось изменить статус пользователя: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось изменить статус пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
