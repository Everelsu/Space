using System;
using System.Windows;

namespace Space
{
    public partial class AddLogDialog : Window
    {
        private readonly TaskItem _task;
        private readonly int _employeeId;

        public AddLogDialog(TaskItem task, int employeeId)
        {
            InitializeComponent();
            _task = task;
            _employeeId = employeeId;
            TaskTitleText.Text = task.Title;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(HoursBox.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal hours)
                || hours <= 0)
            {
                ErrorText.Text = "Введите корректное количество часов";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                await DatabaseService.AddWorkLogAsync(_task.Id, _employeeId, hours, CommentBox.Text.Trim());
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = "Ошибка: " + ex.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
