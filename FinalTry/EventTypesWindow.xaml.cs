using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HrDepartment
{
    public partial class EventTypesWindow : Window
    {
        private readonly EventTypeService _service;
        private readonly bool _isAdmin;
        private List<EventType> _all = new();

        public EventTypesWindow(bool isAdmin)
        {
            InitializeComponent();
            _isAdmin = isAdmin;

            _service = new EventTypeService(DbConfig.ConnectionString);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _all = _service.GetAll();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов событий: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            var q = (SearchBox.Text ?? "").Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(q)
                ? _all
                : _all.Where(t => (t.Name ?? "").ToLower().Contains(q)).ToList();

            GridTypes.ItemsSource = filtered;
        }

        private EventType? Selected => GridTypes.SelectedItem as EventType;

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Добавление доступно только администратору.");
                return;
            }

            var w = new EditEventTypeWindow("Добавить тип события");
            w.Owner = this;

            if (w.ShowDialog() == true)
            {
                try
                {
                    _service.Add(w.ResultName);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления: {ex.Message}");
                }
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Редактирование доступно только администратору.");
                return;
            }

            var item = Selected;
            if (item == null)
            {
                MessageBox.Show("Выберите тип события.");
                return;
            }

            var w = new EditEventTypeWindow("Изменить тип события", item.Name);
            w.Owner = this;

            if (w.ShowDialog() == true)
            {
                try
                {
                    _service.Update(item.Id, w.ResultName);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения: {ex.Message}");
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
            {
                MessageBox.Show("Удаление доступно только администратору.");
                return;
            }

            var item = Selected;
            if (item == null)
            {
                MessageBox.Show("Выберите тип события.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить тип события \"{item.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _service.Delete(item.Id);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }
    }
}
