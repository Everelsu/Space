using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Space
{
    public partial class MainProject : Window
    {
        private readonly UserInfo _user;
        private List<ProjectItem> _all = new List<ProjectItem>();
        private int _editId = -1;

        public MainProject(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            SidebarRole.Text     = (_user.Role ?? "user").ToUpper();
            SidebarUsername.Text = _user.Username;
            Loaded += async (s, e) => await Reload();
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllProjectsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var statusFilter = (FilterStatus?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "";

            var result = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(statusFilter))
                result = result.Where(p => p.Status == statusFilter);
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(p =>
                    p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase)    >= 0 ||
                    p.Manager.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Status.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0);

            var list = result.ToList();
            Grid.ItemsSource = list;

            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} проектов"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, System.Windows.Controls.TextChangedEventArgs e)
            => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);
        private void Filter_Changed(object s, System.Windows.Controls.SelectionChangedEventArgs e)
            => Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");

        private void AddBtn_Click(object s, RoutedEventArgs e)
        {
            _editId = -1;
            FormTitle.Text = "Новый проект";
            SaveBtn.Content = "Сохранить";
            AddName.Text = AddStart.Text = AddDeadline.Text = "";
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((System.Windows.Controls.Button)s).Tag;
            var item = _all.FirstOrDefault(p => p.Id == id);
            if (item == null) return;
            _editId = id;
            FormTitle.Text  = "Редактировать проект";
            SaveBtn.Content = "Обновить";
            AddName.Text     = item.Name;
            AddStart.Text    = item.StartDate == "—" ? "" : item.StartDate;
            AddDeadline.Text = item.Deadline  == "—" ? "" : item.Deadline;
            foreach (System.Windows.Controls.ComboBoxItem ci in AddStatus.Items)
                if (ci.Tag?.ToString() == item.Status) { AddStatus.SelectedItem = ci; break; }
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = Visibility.Visible;
        }

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddName.Text)) { AddError.Text = "Введите название"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddStart.Text, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime start)) { AddError.Text = "Неверный формат начала"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddDeadline.Text, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dl)) { AddError.Text = "Неверный формат дедлайна"; AddError.Visibility = Visibility.Visible; return; }
            try
            {
                var status = (AddStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "active";
                if (_editId > 0)
                    await DatabaseService.UpdateProjectAsync(_editId, AddName.Text.Trim(), start, dl, status);
                else
                    await DatabaseService.AddProjectAsync(AddName.Text.Trim(), "", start, dl, status);
                _editId = -1;
                AddName.Text = AddStart.Text = AddDeadline.Text = "";
                AddError.Visibility = Visibility.Collapsed;
                AddPanel.Visibility = Visibility.Collapsed;
                await Reload();
            }
            catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((System.Windows.Controls.Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить проект?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteProjectAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }

        // Navigation
        private void NavEmployees_Click(object s, RoutedEventArgs e) { new TasksWindow(_user).Show(); Close(); }
        private void NavTeams_Click(object s, RoutedEventArgs e)     { new TeemProject(_user).Show(); Close(); }
        private void NavTasks_Click(object s, RoutedEventArgs e)     { new TaskManageWindow(_user).Show(); Close(); }
        private void NavReports_Click(object s, RoutedEventArgs e)   { new ReportWindows(_user).Show(); Close(); }
        private void Exit_Click(object s, RoutedEventArgs e)         { new Autorisation().Show(); Close(); }
        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object s, RoutedEventArgs e)    => Close();
    }
}
