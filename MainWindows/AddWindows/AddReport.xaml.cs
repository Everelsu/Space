using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddReport : Window
    {
        private readonly WorkLogItem _editItem;
        private readonly bool        _isTester;

        // Timer-flow fields (filled when opened from StopTimer)
        private readonly int     _timerTaskId;
        private readonly string  _timerTaskTitle;   // fallback if task not found in list
        private readonly int     _timerEmployeeId;
        private readonly decimal _timerHours;
        private readonly bool    _isTimerFlow;

        // ── Add (manual) ──────────────────────────────────────────────
        public AddReport(bool isTester = false)
        {
            InitializeComponent();
            _editItem    = null;
            _isTimerFlow = false;
            _isTester    = isTester;
        }

        // ── Edit ──────────────────────────────────────────────────────
        public AddReport(WorkLogItem item)
        {
            InitializeComponent();
            _editItem    = item;
            _isTimerFlow = false;
        }

        // ── Timer flow: pre-filled from stopped timer ─────────────────
        public AddReport(int taskId, string taskTitle, int employeeId, decimal hours)
        {
            InitializeComponent();
            _editItem        = null;
            _isTimerFlow     = true;
            _timerTaskId     = taskId;
            _timerTaskTitle  = taskTitle;
            _timerEmployeeId = employeeId;
            _timerHours      = hours;
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            var taskList     = await DatabaseService.GetTasksDropdownAsync();
            var employeeList = await DatabaseService.GetEmployeesDropdownAsync();
            
            CmbTask.ItemsSource     = taskList;
            CmbEmployee.ItemsSource = employeeList;

            if (_editItem != null)
            {
                // ── Edit mode ──────────────────────────────────────────
                DialogTitle.Text = "Редактировать отчёт";
                SaveBtn.Content  = "Обновить";

                foreach (DropdownItem di in taskList)
                    if (di.Id == _editItem.TaskId) { CmbTask.SelectedItem = di; break; }
                foreach (DropdownItem di in employeeList)
                    if (di.Id == _editItem.EmployeeId) { CmbEmployee.SelectedItem = di; break; }

                CmbTask.IsEnabled     = false;
                CmbEmployee.IsEnabled = false;

                if (DateTime.TryParseExact(_editItem.LogDate, "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime editDt))
                    DpDate.SelectedDate = editDt;
                TxtHours.Text   = _editItem.Hours.ToString(CultureInfo.InvariantCulture);
                TxtComment.Text = _editItem.Comment;
            }
            else if (_isTimerFlow)
            {
                // ── Timer flow mode ────────────────────────────────────
                DialogTitle.Text = "Отчёт о работе";

                // Find task — if not in list (e.g. status changed) add a synthetic entry
                var foundTask = taskList.Find(t => t.Id == _timerTaskId);
                if (foundTask == null && _timerTaskId > 0)
                {
                    foundTask = new DropdownItem { Id = _timerTaskId, Name = _timerTaskTitle ?? "—" };
                    taskList.Add(foundTask);
                    CmbTask.ItemsSource = taskList;   // refresh binding with added item
                }
                CmbTask.SelectedItem = foundTask;
                CmbTask.IsEnabled    = false;         // task is locked — came from timer

                var foundEmp = employeeList.Find(e2 => e2.Id == _timerEmployeeId);
                CmbEmployee.SelectedItem = foundEmp;
                CmbEmployee.IsEnabled    = false;     // employee locked — current user

                TxtHours.Text = _timerHours.ToString("0.##", CultureInfo.InvariantCulture);
                DpDate.SelectedDate = DateTime.Today;
                TxtComment.Focus();
            }
            else
            {
                // ── New manual report ──────────────────────────────────
                DpDate.SelectedDate = DateTime.Today;

                if (_isTester)
                {
                    LblHours.Text = "Часов";
                    TxtHours.Text = "";
                }
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

            decimal hours = 0;
            var hoursText = TxtHours.Text.Trim();
            if (_isTester && string.IsNullOrEmpty(hoursText))
            {
                hours = 0;
            }
            else
            {
                if (!decimal.TryParse(hoursText.Replace(',', '.'), NumberStyles.Any,
                    CultureInfo.InvariantCulture, out hours))
                { ShowError("Введите корректное количество часов"); return; }
                hours = Math.Round(hours, 2, MidpointRounding.AwayFromZero);
                if (hours <= 0)
                { ShowError("Количество часов должно быть больше 0"); return; }
                if (hours > 24)
                { ShowError("Количество часов не может превышать 24 в день"); return; }
            }

            if (!DpDate.SelectedDate.HasValue)
            { ShowError("Выберите дату"); return; }
            var dt = DpDate.SelectedDate.Value;

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
