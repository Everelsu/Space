using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class TaskManageWindow : UserControl
    {
        private List<TaskManageItem> _all      = new List<TaskManageItem>();
        private List<DropdownItem>   _projects  = new List<DropdownItem>();
        private List<DropdownItem>   _employees = new List<DropdownItem>();
        private int _editId = -1;

        public TaskManageWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                _projects  = await DatabaseService.GetProjectsDropdownAsync();
                _employees = await DatabaseService.GetEmployeesDropdownAsync();
                AddProject.ItemsSource  = _projects;
                AddAssignee.ItemsSource = _employees;
                await Reload();
            };
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

        private void AddBtn_Click(object s, RoutedEventArgs e)
        {
            _editId = -1;
            FormTitle.Text  = "Новое задание";
            SaveBtn.Content = "Сохранить";
            AddTitle.Text = AddDeadline.Text = "";
            AddProject.SelectedIndex  = -1;
            AddAssignee.SelectedIndex = -1;
            AddPriority.SelectedIndex = 0;
            AddStatus.SelectedIndex   = 0;
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null) return;

            _editId = id;
            FormTitle.Text  = "Редактировать задание";
            SaveBtn.Content = "Обновить";
            AddTitle.Text = item.Title;
            AddProject.SelectedItem  = _projects.FirstOrDefault(p => p.Id == item.ProjectId);
            AddAssignee.SelectedItem = item.AssigneeId.HasValue
                ? _employees.FirstOrDefault(e2 => e2.Id == item.AssigneeId) : null;
            SetCombo(AddPriority, item.Priority);
            SetCombo(AddStatus,   item.Status);
            AddDeadline.Text = item.Deadline == "—" ? "" : item.Deadline;
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = Visibility.Visible;
        }

        private void SetCombo(ComboBox cb, string value)
        {
            foreach (ComboBoxItem ci in cb.Items)
                if (ci.Tag?.ToString() == value) { cb.SelectedItem = ci; return; }
            cb.SelectedIndex = 0;
        }

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddTitle.Text))
            { AddError.Text = "Введите название задачи"; AddError.Visibility = Visibility.Visible; return; }
            var proj = AddProject.SelectedItem as DropdownItem;
            if (proj == null)
            { AddError.Text = "Выберите проект"; AddError.Visibility = Visibility.Visible; return; }

            var priority = (AddPriority.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "low";
            var status   = (AddStatus.SelectedItem   as ComboBoxItem)?.Tag?.ToString() ?? "open";
            var assignee = AddAssignee.SelectedItem as DropdownItem;
            DateTime? deadline = null;
            if (!string.IsNullOrWhiteSpace(AddDeadline.Text))
            {
                if (!DateTime.TryParseExact(AddDeadline.Text, "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime dl))
                { AddError.Text = "Формат даты: дд.мм.гггг"; AddError.Visibility = Visibility.Visible; return; }
                deadline = dl;
            }

            try
            {
                if (_editId > 0)
                    await DatabaseService.UpdateTaskManageAsync(_editId, AddTitle.Text.Trim(),
                        proj.Id, assignee?.Id, priority, status, deadline);
                else
                    await DatabaseService.AddTaskManageAsync(AddTitle.Text.Trim(),
                        proj.Id, assignee?.Id, priority, status, deadline);
                _editId = -1;
                AddTitle.Text = AddDeadline.Text = "";
                AddProject.SelectedIndex = AddAssignee.SelectedIndex = -1;
                AddError.Visibility = Visibility.Collapsed;
                AddPanel.Visibility = Visibility.Collapsed;
                await Reload();
            }
            catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
        }

        private async void Advance_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null || !item.CanAdvance) return;
            try { await DatabaseService.AdvanceTaskStatusAsync(id, item.Status); await Reload(); }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
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
    }
}
