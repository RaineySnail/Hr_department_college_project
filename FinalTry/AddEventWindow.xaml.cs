using System;
using System.Linq;
using System.Windows;

namespace HrDepartment
{
    public partial class AddEventWindow : Window
    {
        private readonly EventService _service;
        private readonly long _employeeId;

        public AddEventWindow(Employee employee, long? preselectTypeId = null)
        {
            InitializeComponent();

            _service = new EventService(DbConfig.ConnectionString);
            _employeeId = employee.Id;

            EmployeeNameText.Text = $"Сотрудник: {employee.FullName}";

            var types = _service.GetEventTypes();
            TypeCombo.ItemsSource = types;

            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;

            if (preselectTypeId.HasValue)
            {
                TypeCombo.SelectedItem = types.FirstOrDefault(t => t.Id == preselectTypeId.Value);
            }
            else
            {
                TypeCombo.SelectedIndex = 0;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (TypeCombo.SelectedItem is not EventType type)
            {
                MessageBox.Show("Выберите тип события!");
                return;
            }

            if (StartDatePicker.SelectedDate is null || EndDatePicker.SelectedDate is null)
            {
                MessageBox.Show("Выберите даты начала и окончания!");
                return;
            }

            var start = StartDatePicker.SelectedDate.Value;
            var end = EndDatePicker.SelectedDate.Value;

            if (end < start)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала!");
                return;
            }

            try
            {
                _service.AddEvent(_employeeId, start, end, type.Id, ReasonBox.Text);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения события: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
