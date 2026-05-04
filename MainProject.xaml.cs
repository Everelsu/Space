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

        public MainProject(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => await Reload();
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllProjectsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string filter = "")
        {
            if (Grid == null) return;
            Grid.ItemsSource = string.IsNullOrWhiteSpace(filter)
                ? _all
                : _all.Where(p => p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                               || p.Manager.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                               || p.Status.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, System.Windows.Controls.TextChangedEventArgs e)
            => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);

        private void AddBtn_Click(object s, RoutedEventArgs e)
            => AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddName.Text)) { AddError.Text = "Введите название"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddStart.Text, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime start)) { AddError.Text = "Неверный формат начала"; AddError.Visibility = Visibility.Visible; return; }
            if (!DateTime.TryParseExact(AddDeadline.Text, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dl)) { AddError.Text = "Неверный формат дедлайна"; AddError.Visibility = Visibility.Visible; return; }
            try
            {
                var status = (AddStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "active";
                await DatabaseService.AddProjectAsync(AddName.Text.Trim(), "", start, dl, status);
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
        private void NavReports_Click(object s, RoutedEventArgs e)   { new ReportWindows(_user).Show(); Close(); }
        private void Exit_Click(object s, RoutedEventArgs e)         { new Autorisation().Show(); Close(); }
        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object s, RoutedEventArgs e)    => Close();
    }
}
