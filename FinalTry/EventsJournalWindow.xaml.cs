using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HrDepartment
{
    public partial class EventsJournalWindow : Window
    {
        private readonly EventService _service;

        public EventsJournalWindow()
        {
            InitializeComponent();

            _service = new EventService(DbConfig.ConnectionString);

            var types = _service.GetEventTypes();
            TypeCombo.ItemsSource = types;

            LoadEvents();
        }

        private void LoadEvents()
        {
            long? typeId = (TypeCombo.SelectedItem as EventType)?.Id;
            var list = _service.GetEvents(
                FromDate.SelectedDate,
                ToDate.SelectedDate,
                typeId,
                EmployeeQuery.Text
            );

            GridEvents.ItemsSource = list;
        }

        private void Search_Click(object sender, RoutedEventArgs e) => LoadEvents();

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            FromDate.SelectedDate = null;
            ToDate.SelectedDate = null;
            TypeCombo.SelectedItem = null;
            EmployeeQuery.Text = "";
            LoadEvents();
        }
    }
}
