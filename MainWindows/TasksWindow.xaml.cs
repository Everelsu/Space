using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class TasksWindow : UserControl
    {
        private List<EmployeeItem> _all = new List<EmployeeItem>();
        private readonly UserInfo _user;

        public TasksWindow() : this(null) { }

        public TasksWindow(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => { ApplyRole(); await Reload(); };
        }

        // Admin and manager can create / edit / delete employees
        private bool CanManage => _user?.Role == "admin" || _user?.Role == "manager";

        private void ApplyRole()
        {
            if (!CanManage)
            {
                AddBtn.Visibility     = Visibility.Collapsed;
                ColActions.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try { _all = await DatabaseService.GetAllEmployeesAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var roleFilter = (FilterRole?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var result = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(roleFilter))
                result = result.Where(p => p.Role == roleFilter);
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(p =>
                    p.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0 ||
                    p.Position.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0 ||
                    p.Team.IndexOf(search, StringComparison.OrdinalIgnoreCase)      >= 0 ||
                    p.Username.IndexOf(search, StringComparison.OrdinalIgnoreCase)  >= 0);

            var list = result.ToList();
            Grid.ItemsSource = list;
            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} работников"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, TextChangedEventArgs e) => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);
        private void Filter_Changed(object s, SelectionChangedEventArgs e) => Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");

        private async void AddBtn_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddWindows.AddWorker { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(p => p.Id == id);
            if (item == null) return;
            var dlg = new AddWindows.AddWorker(item) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить работника?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteEmployeeAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }
    }
}
