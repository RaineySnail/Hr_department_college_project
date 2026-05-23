using System;
using System.Windows;

namespace HrDepartment
{
    public partial class EditEventTypeWindow : Window
    {
        public string ResultName { get; private set; } = "";
        private readonly string _mode;

        public EditEventTypeWindow(string modeTitle, string? initialName = null)
        {
            InitializeComponent();
            _mode = modeTitle;

            Title = modeTitle;
            TitleText.Text = modeTitle;

            if (!string.IsNullOrWhiteSpace(initialName))
                NameBox.Text = initialName;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim() ?? "";

            if (name.Length < 2)
            {
                MessageBox.Show("Введите название (минимум 2 символа).");
                return;
            }

            ResultName = name;
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
