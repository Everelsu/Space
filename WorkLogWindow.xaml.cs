using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Space
{
    public partial class WorkLogWindow : Window
    {
        private readonly UserInfo _user;
        private readonly int _employeeId;
        private TaskItem _selectedTask;

        public WorkLogWindow(UserInfo user, int employeeId)
        {
            InitializeComponent();
            _user = user;
            _employeeId = employeeId;
            DateBox.Text = DateTime.Today.ToString("dd.MM.yyyy");

            Loaded += async (s, e) =>
            {
                var tasks = await DatabaseService.GetMyTasksAsync(_employeeId, _user.Role);
                TaskListPanel.ItemsSource = tasks;
            };
        }

        private async void SelectTask_Click(object sender, RoutedEventArgs e)
        {
            _selectedTask = (TaskItem)((System.Windows.Controls.Button)sender).Tag;

            SelectedTaskTitle.Text = _selectedTask.Title;
            SelectedTaskMeta.Text  = $"{_selectedTask.ProjectName}  •  {_selectedTask.Priority}  •  до {_selectedTask.DeadlineText}";
            SaveBtn.IsEnabled = true;
            FormError.Visibility = Visibility.Collapsed;

            await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            var logs = await DatabaseService.GetTaskLogsAsync(_selectedTask.Id);
            LogHistory.ItemsSource = logs;
            NoLogsText.Visibility = logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void SaveLog_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null) return;

            if (string.IsNullOrWhiteSpace(CommentBox.Text))
            {
                ShowError("Заполните поле «Что сделано»");
                return;
            }

            if (!decimal.TryParse(HoursBox.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal hours) || hours <= 0)
            {
                ShowError("Введите корректное количество часов");
                return;
            }

            if (!DateTime.TryParseExact(DateBox.Text, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime logDate))
            {
                ShowError("Формат даты: дд.мм.гггг");
                return;
            }

            SaveBtn.IsEnabled = false;
            FormError.Visibility = Visibility.Collapsed;

            try
            {
                await DatabaseService.AddWorkLogAsync(_selectedTask.Id, _employeeId, hours,
                    CommentBox.Text.Trim(), logDate);

                CommentBox.Text = "";
                HoursBox.Text = "1";
                DateBox.Text = DateTime.Today.ToString("dd.MM.yyyy");

                await LoadHistory();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
            }
            finally
            {
                SaveBtn.IsEnabled = true;
            }
        }

        private void ShowError(string msg)
        {
            FormError.Text = msg;
            FormError.Visibility = Visibility.Visible;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}
