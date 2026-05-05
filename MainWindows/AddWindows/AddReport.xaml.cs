using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddReport : Window
    {
        private readonly WorkLogItem _editItem;

        // ── Add ──────────────────────────────────────────────────────
        public AddReport()
        {
            InitializeComponent();
            _editItem = null;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddReport(WorkLogItem item)
        {
            InitializeComponent();
            _editItem = item;
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            CmbTask.ItemsSource     = await DatabaseService.GetTasksDropdownAsync();
            CmbEmployee.ItemsSource = await DatabaseService.GetEmployeesDropdownAsync();

            if (_editItem != null)
            {
                DialogTitle.Text = "Редактировать отчёт";
                SaveBtn.Content  = "Обновить";

                foreach (DropdownItem di in CmbTask.Items)
                    if (di.Id == _editItem.TaskId) { CmbTask.SelectedItem = di; break; }
                foreach (DropdownItem di in CmbEmployee.Items)
                    if (di.Id == _editItem.EmployeeId) { CmbEmployee.SelectedItem = di; break; }

                CmbTask.IsEnabled     = false;
                CmbEmployee.IsEnabled = false;

                TxtDate.Text    = _editItem.LogDate;
                TxtHours.Text   = _editItem.Hours.ToString(CultureInfo.InvariantCulture);
                TxtComment.Text = _editItem.Comment;
            }
            else
            {
                TxtDate.Text = DateTime.Today.ToString("dd.MM.yyyy");
            }
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (!decimal.TryParse(TxtHours.Text.Replace(',', '.'), NumberStyles.Any,
                CultureInfo.InvariantCulture, out decimal hours) || hours <= 0)
            { ShowError("Введите корректное количество часов (больше 0)"); return; }

            if (!DateTime.TryParseExact(TxtDate.Text.Trim(), "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            { ShowError("Неверный формат даты. Используйте дд.мм.гггг"); return; }

            SaveBtn.IsEnabled = false;
            try
            {
                if (_editItem != null)
                {
                    await DatabaseService.UpdateWorkLogAsync(_editItem.Id, hours,
                        TxtComment.Text.Trim(), dt);
                }
                else
                {
                    var task = CmbTask.SelectedItem as DropdownItem;
                    var emp  = CmbEmployee.SelectedItem as DropdownItem;
                    if (task == null || emp == null)
                    { ShowError("Выберите задачу и работника"); SaveBtn.IsEnabled = true; return; }

                    await DatabaseService.AddWorkLogAsync(task.Id, emp.Id, hours,
                        TxtComment.Text.Trim(), dt);
                }
                DialogResult = true;
            }
            catch (Exception ex) { ShowError(ex.Message); SaveBtn.IsEnabled = true; }
        }

        private void ShowError(string msg)
        {
            ErrorText.Text       = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
