using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Space
{
    public partial class ReportWindows : Window
    {
        private readonly UserInfo _user;
        private List<WorkLogItem> _all = new List<WorkLogItem>();

        public ReportWindows(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) =>
            {
                await Reload();
                AddTask.ItemsSource     = await DatabaseService.GetTasksDropdownAsync();
                AddEmployee.ItemsSource = await DatabaseService.GetEmployeesDropdownAsync();
                AddDate.Text = DateTime.Today.ToString("dd.MM.yyyy");
            };
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllWorkLogsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string filter = "")
        {
            if (Grid == null) return;
            Grid.ItemsSource = string.IsNullOrWhiteSpace(filter)
                ? _all
                : _all.Where(w => w.Task.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                               || w.Employee.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                               || w.Comment.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, System.Windows.Controls.TextChangedEventArgs e)
            => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);

        private void AddBtn_Click(object s, RoutedEventArgs e)
            => AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            var task = AddTask.SelectedItem as DropdownItem;
            var emp  = AddEmployee.SelectedItem as DropdownItem;
            if (task == null || emp == null) { AddError.Text = "Выберите задачу и работника"; AddError.Visibility = Visibility.Visible; return; }
            if (!decimal.TryParse(AddHours.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal hours) || hours <= 0)
            { AddError.Text = "Введите корректные часы"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddDate.Text, "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
            { AddError.Text = "Формат даты: дд.мм.гггг"; AddError.Visibility = Visibility.Visible; return; }

            try
            {
                await DatabaseService.AddWorkLogAsync(task.Id, emp.Id, hours, AddComment.Text.Trim(), dt);
                AddHours.Text = "1"; AddComment.Text = "";
                AddDate.Text = DateTime.Today.ToString("dd.MM.yyyy");
                AddError.Visibility = Visibility.Collapsed;
                AddPanel.Visibility = Visibility.Collapsed;
                await Reload();
            }
            catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((System.Windows.Controls.Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить отчёт?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteWorkLogAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }

        private void NavProjects_Click(object s, RoutedEventArgs e)  { new MainProject(_user).Show(); Close(); }
        private void NavEmployees_Click(object s, RoutedEventArgs e) { new TasksWindow(_user).Show(); Close(); }
        private void NavTeams_Click(object s, RoutedEventArgs e)     { new TeemProject(_user).Show(); Close(); }
        private void Exit_Click(object s, RoutedEventArgs e)         { new Autorisation().Show(); Close(); }
        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object s, RoutedEventArgs e)    => Close();
    }
}
