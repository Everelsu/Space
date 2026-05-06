using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class MainProject : UserControl
    {
        private List<ProjectItem> _all = new List<ProjectItem>();
        private readonly UserInfo _user;

        public MainProject() : this(null) { }

        public MainProject(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => { ApplyRole(); await Reload(); };
        }

        // admin + manager can create / edit / delete projects
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
            try { _all = await DatabaseService.GetAllProjectsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var statusFilter = (FilterStatus?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var result = _all.AsEnumerable();

            if (!string.IsNullOrEmpty(statusFilter))
                result = result.Where(p => p.Status == statusFilter);
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(p =>
                    p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase)    >= 0 ||
                    p.Manager.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.StatusLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

            var list = result.ToList();
            Grid.ItemsSource = list;
            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} проектов"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, TextChangedEventArgs e) => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);
        private void Filter_Changed(object s, SelectionChangedEventArgs e) => Apply(SearchBox?.Text.StartsWith("🔍") == true ? "" : SearchBox?.Text ?? "");

        private async void AddBtn_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddWindows.AddProject(_user) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(p => p.Id == id);
            if (item == null) return;
            var dlg = new AddWindows.AddProject(item, _user) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить проект?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteProjectAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
