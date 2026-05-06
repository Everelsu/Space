using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddProject : Window
    {
        private readonly int      _editId;
        private readonly int?     _editManagerId;
        private readonly UserInfo _user;

        // ── Add ──────────────────────────────────────────────────────
        public AddProject(UserInfo user = null)
        {
            InitializeComponent();
            _editId = -1;
            _user   = user;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddProject(ProjectItem item, UserInfo user = null)
        {
            InitializeComponent();
            _editId        = item.Id;
            _editManagerId = item.ManagerId;
            _user          = user;
            DialogTitle.Text = "Редактировать проект";
            SaveBtn.Content  = "Обновить";
            TxtName.Text     = item.Name;
            if (item.StartDate != "—" && DateTime.TryParseExact(item.StartDate, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime s))
                DpStart.SelectedDate = s;
            if (item.Deadline != "—" && DateTime.TryParseExact(item.Deadline, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime d))
                DpDeadline.SelectedDate = d;
            foreach (ComboBoxItem ci in CmbStatus.Items)
                if (ci.Tag?.ToString() == item.Status) { CmbStatus.SelectedItem = ci; break; }
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            var employees = await DatabaseService.GetEmployeesDropdownAsync();
            CmbManager.ItemsSource = employees;

            if (_editManagerId.HasValue)
            {
                foreach (DropdownItem di in employees)
                    if (di.Id == _editManagerId.Value) { CmbManager.SelectedItem = di; break; }
            }
            else if (_user?.EmployeeId.HasValue == true)
            {
                foreach (DropdownItem di in employees)
                    if (di.Id == _user.EmployeeId.Value) { CmbManager.SelectedItem = di; break; }
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

            if (string.IsNullOrWhiteSpace(TxtName.Text))
            { ShowError("Введите название проекта"); return; }
            if (!DpStart.SelectedDate.HasValue)
            { ShowError("Выберите дату начала"); return; }
            if (!DpDeadline.SelectedDate.HasValue)
            { ShowError("Выберите дедлайн"); return; }
            var start = DpStart.SelectedDate.Value;
            var dl    = DpDeadline.SelectedDate.Value;

            var status    = (CmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "active";
            var managerId = (CmbManager.SelectedItem as DropdownItem)?.Id;
            SaveBtn.IsEnabled = false;
            try
            {
                if (_editId > 0)
                    await DatabaseService.UpdateProjectAsync(_editId, TxtName.Text.Trim(), start, dl, status, managerId);
                else
                    await DatabaseService.AddProjectAsync(TxtName.Text.Trim(),
                        TxtDesc.Text.Trim(), start, dl, status, managerId);
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
