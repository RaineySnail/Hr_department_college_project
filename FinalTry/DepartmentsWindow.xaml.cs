using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace HrDepartment
{
    public partial class DepartmentsWindow : Window
    {
        private readonly string _connectionString = DbConfig.ConnectionString;
        private DataTable? _departmentsTable;

        public DepartmentsWindow()
        {
            InitializeComponent();

            SearchTextBox.Text = "Поиск по названию...";
            SearchTextBox.Foreground = Brushes.Gray;

            LoadDepartments();
        }

        private void LoadDepartments()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string query = "SELECT * FROM структурное_подразделение ORDER BY Название_подразделения";
                using var adapter = new MySqlDataAdapter(query, connection);

                _departmentsTable = new DataTable();
                adapter.Fill(_departmentsTable);
                DepartmentsGrid.ItemsSource = _departmentsTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отделов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск по названию...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск по названию...";
                SearchTextBox.Foreground = Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск по названию...")
                return;

            if (_departmentsTable is not null)
            {
                string searchText = SearchTextBox.Text.ToLower();
                _departmentsTable.DefaultView.RowFilter =
                    $"Название_подразделения LIKE '%{searchText}%' OR описание LIKE '%{searchText}%'";
            }
        }

        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new DepartmentEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadDepartments();
                MessageBox.Show("Отдел успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is DataRowView row)
            {
                var editWindow = new DepartmentEditWindow(row);
                if (editWindow.ShowDialog() == true)
                {
                    LoadDepartments();
                    MessageBox.Show("Отдел успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Выберите отдел для редактирования!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите отдел для удаления!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int departmentId = Convert.ToInt32(row["id_department"]);
            string departmentName = row["Название_подразделения"].ToString() ?? "";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                const string checkQuery = "SELECT COUNT(*) FROM сотрудник WHERE id_подразделения = @departmentId";
                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@departmentId", departmentId);

                int employeeCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (employeeCount > 0)
                {
                    MessageBox.Show($"Нельзя удалить отдел! В отделе '{departmentName}' работает {employeeCount} сотрудник(ов).",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Удалить отдел '{departmentName}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                const string deleteQuery = "DELETE FROM структурное_подразделение WHERE id_department = @id";
                using var deleteCmd = new MySqlCommand(deleteQuery, connection);
                deleteCmd.Parameters.AddWithValue("@id", departmentId);
                deleteCmd.ExecuteNonQuery();

                LoadDepartments();
                MessageBox.Show("Отдел удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DepartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
