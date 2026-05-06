using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class TaskManageWindow : UserControl
    {
        private List<TaskManageItem> _all = new List<TaskManageItem>();
        private readonly UserInfo _user;

        // Parameterless ctor keeps XAML designer happy
        public TaskManageWindow() : this(null) { }

        public TaskManageWindow(UserInfo user)
        {
            InitializeComponent();
            _user = user;

            Loaded += async (s, e) =>
            {
                ApplyRole();
                await Reload();
            };
        }

        private string Role      => _user?.Role?.ToLower() ?? "";
        private bool IsAdmin     => Role == "admin";
        private bool IsManager   => Role == "manager";
        private bool IsDeveloper => Role == "developer";
        private bool IsTester    => Role == "tester";

        private void ApplyRole()
        {
            bool canManage = IsAdmin || IsManager;
            AddBtn.Visibility    = canManage ? Visibility.Visible : Visibility.Collapsed;
            ColActions.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;

            ColTimer.Visibility = (IsAdmin || IsDeveloper) ? Visibility.Visible : Visibility.Collapsed;

            // Status advance — tester sees separate column (testing→closed only)
            ColAdvanceDev.Visibility    = IsTester ? Visibility.Collapsed : Visibility.Visible;
            ColAdvanceTester.Visibility = IsTester ? Visibility.Visible   : Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllTasksManageAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var statusFilter   = (FilterStatus?.SelectedItem   as ComboBoxItem)?.Tag?.ToString() ?? "";
            var priorityFilter = (FilterPriority?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var result = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(statusFilter))
                result = result.Where(t => t.Status == statusFilter);
            if (!string.IsNullOrEmpty(priorityFilter))
                result = result.Where(t => t.Priority == priorityFilter);
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(t =>
                    t.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase)    >= 0 ||
                    t.Project.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0 ||
                    t.Assignee.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

            var list = result.ToList();
            Grid.ItemsSource = list;
            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} заданий"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, TextChangedEventArgs e) => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);
        private void Filter_Changed(object s, SelectionChangedEventArgs e) => Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");

        private async void AddBtn_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddWindows.AddTask { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null) return;
            var dlg = new AddWindows.AddTask(item) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Advance_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null || !item.CanAdvance) return;
            try { await DatabaseService.AdvanceTaskStatusAsync(id, item.Status); await Reload(); }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        private void StartTimer_Click(object s, RoutedEventArgs e)
        {
            if (TimerService.IsRunning)
            {
                MessageBox.Show(
                    $"Сейчас уже запущен таймер для задачи:\n\"{TimerService.ActiveTaskTitle}\"\n\nСначала остановите его.",
                    "Таймер уже запущен", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null) return;

            TimerService.Start(id, item.Title);
            Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");
        }

        private void StopTimer_Click(object s, RoutedEventArgs e)
        {
            if (!TimerService.IsRunning) return;

            var taskId    = TimerService.ActiveTaskId;
            var taskTitle = TimerService.ActiveTaskTitle;
            var empId     = _user?.EmployeeId;
            var hours     = TimerService.Stop();

            Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");

            AddWindows.AddReport dlg;
            if (empId.HasValue)
                dlg = new AddWindows.AddReport(taskId, taskTitle, empId.Value, hours);
            else
                dlg = new AddWindows.AddReport();

            dlg.Owner = Window.GetWindow(this);
            dlg.ShowDialog();
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить задание?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteTaskManageAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
