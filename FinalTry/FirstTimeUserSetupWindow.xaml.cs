using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;

namespace HrDepartment
{
    public partial class FirstTimeUserSetupWindow : Window
    {
        private readonly User _user;
        public long? CreatedEmployeeId { get; private set; }

        public FirstTimeUserSetupWindow(User user)
        {
            InitializeComponent();
            _user = user;
            FullNameTextBox.Text = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
            SignatureTextBox.Text = FullNameTextBox.Text;
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-18);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ConsentCheckBox.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show("Без согласия на обработку персональных данных вход в систему невозможен.");
                return;
            }

            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) || BirthDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Заполните обязательные поля: ФИО и дату рождения.");
                return;
            }

            if (string.IsNullOrWhiteSpace(SignatureTextBox.Text))
            {
                MessageBox.Show("Укажите подпись для согласия.");
                return;
            }

            string gender = ((GenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty).Trim();

            using var connection = new MySqlConnection(DbConfig.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                long employeeId;
                if (_user.EmployeeId.HasValue)
                {
                    employeeId = _user.EmployeeId.Value;
                    using var updateEmployee = new MySqlCommand(@"
UPDATE сотрудник
SET ФИО=@fullName,
    Адрес=@address,
    Дата_рождения=@birthDate,
    Номер_телефона=@personalPhone,
    Номер_телефона_раб=@workPhone,
    Корпоративный_email=@email,
    Пол=@gender,
    Место_рождения=@placeOfBirth,
    Паспортные_данные=@passport,
    СНИЛС=@snils,
    ИНН=@inn,
    Гражданство=@citizenship,
    Образование=@education,
    Дата_приема=COALESCE(Дата_приема, CURDATE())
WHERE id_employee=@id;", connection, transaction);
                    FillEmployeeParameters(updateEmployee, gender);
                    updateEmployee.Parameters.AddWithValue("@id", employeeId);
                    updateEmployee.ExecuteNonQuery();
                }
                else
                {
                    int defaultDepartmentId = GetFirstLookupId(connection, transaction, "SELECT id_department FROM структурное_подразделение ORDER BY id_department LIMIT 1;");
                    int defaultPositionId = GetFirstLookupId(connection, transaction, "SELECT id_position FROM должность ORDER BY id_position LIMIT 1;");

                    using var insertEmployee = new MySqlCommand(@"
INSERT INTO сотрудник
(ФИО, Адрес, Дата_рождения, Номер_телефона, Номер_телефона_раб, Кабинет, id_подразделения, id_должности, Корпоративный_email, В_отпуске, Пол, Место_рождения, Паспортные_данные, СНИЛС, ИНН, Гражданство, Образование, Дата_приема, Примечание)
VALUES
(@fullName, @address, @birthDate, @personalPhone, @workPhone, '-', @departmentId, @positionId, @email, 0, @gender, @placeOfBirth, @passport, @snils, @inn, @citizenship, @education, CURDATE(), 'Создано пользователем при первом входе');
SELECT LAST_INSERT_ID();", connection, transaction);
                    FillEmployeeParameters(insertEmployee, gender);
                    insertEmployee.Parameters.AddWithValue("@departmentId", defaultDepartmentId);
                    insertEmployee.Parameters.AddWithValue("@positionId", defaultPositionId);
                    employeeId = Convert.ToInt64(insertEmployee.ExecuteScalar());

                    using var updateUser = new MySqlCommand("UPDATE users SET employee_id=@employeeId, ФИО=@fullName WHERE id=@userId;", connection, transaction);
                    updateUser.Parameters.AddWithValue("@employeeId", employeeId);
                    updateUser.Parameters.AddWithValue("@fullName", FullNameTextBox.Text.Trim());
                    updateUser.Parameters.AddWithValue("@userId", _user.Id);
                    updateUser.ExecuteNonQuery();
                }

                using var deleteConsent = new MySqlCommand("DELETE FROM согласие_на_обработку_пдн WHERE id_сотрудника=@employeeId;", connection, transaction);
                deleteConsent.Parameters.AddWithValue("@employeeId", employeeId);
                deleteConsent.ExecuteNonQuery();

                using var insertConsent = new MySqlCommand(@"
INSERT INTO согласие_на_обработку_пдн (id_сотрудника, consent_given, signature_name, signed_at)
VALUES (@employeeId, 1, @signature, CURDATE());", connection, transaction);
                insertConsent.Parameters.AddWithValue("@employeeId", employeeId);
                insertConsent.Parameters.AddWithValue("@signature", SignatureTextBox.Text.Trim());
                insertConsent.ExecuteNonQuery();

                transaction.Commit();

                _user.EmployeeId = employeeId;
                _user.FullName = FullNameTextBox.Text.Trim();
                _user.HasPersonalDataConsent = true;
                CreatedEmployeeId = employeeId;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Не удалось завершить первый вход.\n{ex.Message}");
            }
        }

        private void FillEmployeeParameters(MySqlCommand command, string gender)
        {
            command.Parameters.AddWithValue("@fullName", FullNameTextBox.Text.Trim());
            command.Parameters.AddWithValue("@address", AddressTextBox.Text.Trim());
            command.Parameters.AddWithValue("@birthDate", BirthDatePicker.SelectedDate!.Value.Date);
            command.Parameters.AddWithValue("@personalPhone", PersonalPhoneTextBox.Text.Trim());
            command.Parameters.AddWithValue("@workPhone", WorkPhoneTextBox.Text.Trim());
            command.Parameters.AddWithValue("@email", EmailTextBox.Text.Trim());
            command.Parameters.AddWithValue("@gender", gender);
            command.Parameters.AddWithValue("@placeOfBirth", PlaceOfBirthTextBox.Text.Trim());
            command.Parameters.AddWithValue("@passport", PassportTextBox.Text.Trim());
            command.Parameters.AddWithValue("@snils", SnilsTextBox.Text.Trim());
            command.Parameters.AddWithValue("@inn", InnTextBox.Text.Trim());
            command.Parameters.AddWithValue("@citizenship", CitizenshipTextBox.Text.Trim());
            command.Parameters.AddWithValue("@education", EducationTextBox.Text.Trim());
        }

        private static int GetFirstLookupId(MySqlConnection connection, MySqlTransaction transaction, string sql)
        {
            using var cmd = new MySqlCommand(sql, connection, transaction);
            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("В базе нет справочных данных по подразделениям или должностям.");
            return Convert.ToInt32(result);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
