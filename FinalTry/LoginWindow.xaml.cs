using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Input;

namespace HrDepartment
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString = DbConfig.ConnectionString;

        public User? CurrentUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }



        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(sender, new RoutedEventArgs());
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            using MySqlConnection connection = new MySqlConnection(_connectionString);
            try
            {
                connection.Open();
                string query = @"
SELECT u.id,
       u.username,
       u.id_роли,
       r.название_роли AS role_name,
       u.ФИО,
       u.активен,
       u.employee_id,
       CASE WHEN c.id IS NULL THEN 0 ELSE 1 END AS has_consent
FROM users u
LEFT JOIN роли r ON u.id_роли = r.id_роли
LEFT JOIN согласие_на_обработку_пдн c ON c.id_сотрудника = u.employee_id AND c.consent_given = 1
WHERE u.username = @username
  AND u.password = @password
  AND u.активен = TRUE
LIMIT 1;";

                using MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", txtUsername.Text.Trim());
                cmd.Parameters.AddWithValue("@password", txtPassword.Password);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    MessageBox.Show("Неверные учётные данные или пользователь заблокирован.");
                    return;
                }

                CurrentUser = new User
                {
                    Id = reader.GetInt32("id"),
                    Username = reader.GetString("username"),
                    RoleId = reader.GetInt32("id_роли"),
                    RoleName = reader.GetStringSafe("role_name"),
                    FullName = reader.GetStringSafe("ФИО"),
                    IsActive = reader.GetBoolean("активен"),
                    EmployeeId = reader.IsDBNull(reader.GetOrdinal("employee_id")) ? null : reader.GetInt64("employee_id"),
                    HasPersonalDataConsent = !reader.IsDBNull(reader.GetOrdinal("has_consent")) && reader.GetInt32("has_consent") == 1
                };
                reader.Close();

                if (CurrentUser.IsSelfServiceUser && (!CurrentUser.EmployeeId.HasValue || !CurrentUser.HasPersonalDataConsent))
                {
                    var setupWindow = new FirstTimeUserSetupWindow(CurrentUser)
                    {
                        Owner = this
                    };

                    bool? setupResult = setupWindow.ShowDialog();
                    if (setupResult != true)
                    {
                        MessageBox.Show("Вход отменён. Пользователь должен дать согласие и заполнить личные данные.");
                        return;
                    }
                }

                var main = new MainWindow(CurrentUser);
                main.Show();
                Close();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }
    }
}
