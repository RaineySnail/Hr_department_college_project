using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HrDepartment
{
    public partial class UserEditWindow : Window
    {
        private readonly bool _isEditMode;

        public User ResultUser { get; private set; } = new();

        public UserEditWindow(string title, IEnumerable<UserRoleOption> roles, User? existingUser = null)
        {
            InitializeComponent();

            _isEditMode = existingUser is not null;
            Title = title;
            WindowTitleText.Text = title;

            var roleItems = roles.ToList();
            RoleComboBox.ItemsSource = roleItems;

            if (_isEditMode && existingUser is not null)
            {
                UsernameBox.Text = existingUser.Username;
                FullNameBox.Text = existingUser.FullName;
                RoleComboBox.SelectedValue = existingUser.RoleId;
                IsActiveCheckBox.IsChecked = existingUser.IsActive;
                PasswordHintText.Text = "Оставьте поле пустым, если пароль менять не нужно.";
            }
            else
            {
                IsActiveCheckBox.IsChecked = true;
                PasswordHintText.Text = "Введите пароль для нового пользователя.";
            }

            if (RoleComboBox.SelectedIndex < 0 && roleItems.Count > 0)
                RoleComboBox.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string fullName = FullNameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (username.Length < 3)
            {
                MessageBox.Show("Логин должен содержать минимум 3 символа.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (fullName.Length < 5)
            {
                MessageBox.Show("Введите ФИО пользователя.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RoleComboBox.SelectedItem is not UserRoleOption role)
            {
                MessageBox.Show("Выберите роль пользователя.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isEditMode && password.Length < 4)
            {
                MessageBox.Show("Пароль нового пользователя должен содержать минимум 4 символа.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultUser = new User
            {
                Username = username,
                FullName = fullName,
                Password = password,
                RoleId = role.Id,
                RoleName = role.Name,
                IsActive = IsActiveCheckBox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
