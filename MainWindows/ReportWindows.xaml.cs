using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class ReportWindows : UserControl
    {
        private List<WorkLogItem> _all = new List<WorkLogItem>();
        private int _editId = -1;
        private readonly UserInfo _user;

        public ReportWindows() : this(null) { }

        public ReportWindows(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) =>
            {
                ApplyRole();
                await Reload();
                AddTask.ItemsSource     = await DatabaseService.GetTasksDropdownAsync();
                AddEmployee.ItemsSource = await DatabaseService.GetEmployeesDropdownAsync();
                AddDate.Text = DateTime.Today.ToString("dd.MM.yyyy");
            };
        }

        private bool IsAdmin => _user?.Role == "admin";

        private void ApplyRole()
        {
            // Everyone can add reports; only admin can edit / delete
            if (!IsAdmin)
                ColActions.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllWorkLogsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var list = string.IsNullOrWhiteSpace(search)
                ? _all
                : _all.Where(w =>
                    w.Task.IndexOf(search, StringComparison.OrdinalIgnoreCase)     >= 0 ||
                    w.Employee.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    w.Comment.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0).ToList();

            Grid.ItemsSource = list;

            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} записей"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, System.Windows.Controls.TextChangedEventArgs e)
            => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);

        private void AddBtn_Click(object s, RoutedEventArgs e)
        {
            _editId = -1;
            FormTitle.Text  = "Новый отчёт";
            SaveBtn.Content = "Сохранить";
            AddTask.IsEnabled     = true;
            AddEmployee.IsEnabled = true;
            AddTask.SelectedIndex = AddEmployee.SelectedIndex = -1;
            AddHours.Text   = "1";
            AddComment.Text = "";
            AddDate.Text    = DateTime.Today.ToString("dd.MM.yyyy");
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(w => w.Id == id);
            if (item == null) return;

            _editId = id;
            FormTitle.Text  = "Редактировать отчёт";
            SaveBtn.Content = "Обновить";

            AddTask.IsEnabled     = false;
            AddEmployee.IsEnabled = false;

            foreach (DropdownItem di in AddTask.Items)
                if (di.Id == item.TaskId) { AddTask.SelectedItem = di; break; }
            foreach (DropdownItem di in AddEmployee.Items)
                if (di.Id == item.EmployeeId) { AddEmployee.SelectedItem = di; break; }

            AddHours.Text   = item.Hours.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AddComment.Text = item.Comment;
            AddDate.Text    = item.LogDate;

            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = Visibility.Visible;
        }

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (!decimal.TryParse(AddHours.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal hours) || hours <= 0)
            { AddError.Text = "Введите корректные часы"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddDate.Text, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
            { AddError.Text = "Формат даты: дд.мм.гггг"; AddError.Visibility = Visibility.Visible; return; }

            if (_editId > 0)
            {
                try
                {
                    await DatabaseService.UpdateWorkLogAsync(_editId, hours, AddComment.Text.Trim(), dt);
                    _editId = -1;
                    AddPanel.Visibility = Visibility.Collapsed;
                    AddError.Visibility = Visibility.Collapsed;
                    await Reload();
                }
                catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
            }
            else
            {
                var task = AddTask.SelectedItem as DropdownItem;
                var emp  = AddEmployee.SelectedItem as DropdownItem;
                if (task == null || emp == null) { AddError.Text = "Выберите задачу и работника"; AddError.Visibility = Visibility.Visible; return; }
                try
                {
                    await DatabaseService.AddWorkLogAsync(task.Id, emp.Id, hours, AddComment.Text.Trim(), dt);
                    AddHours.Text = "1"; AddComment.Text = "";
                    AddDate.Text  = DateTime.Today.ToString("dd.MM.yyyy");
                    AddError.Visibility = Visibility.Collapsed;
                    AddPanel.Visibility = Visibility.Collapsed;
                    await Reload();
                }
                catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
            }
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить отчёт?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteWorkLogAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }
    }
}
