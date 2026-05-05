using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Space
{
    public partial class TeemProject : UserControl
    {
        private List<TeamItem> _all = new List<TeamItem>();
        private readonly UserInfo _user;

        public TeemProject() : this(null) { }

        public TeemProject(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => { ApplyRole(); await Reload(); };
        }

        // Admin and manager can create / edit / delete teams
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
            try { _all = await DatabaseService.GetAllTeamsAsync(); Apply(); }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        private void Apply(string search = "")
        {
            if (Grid == null) return;

            var list = string.IsNullOrWhiteSpace(search)
                ? _all
                : _all.Where(t =>
                    t.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.Lead.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            Grid.ItemsSource = list;
            if (CountLabel != null)
                CountLabel.Text = list.Count == _all.Count
                    ? $"{_all.Count} команд"
                    : $"{list.Count} из {_all.Count}";
        }

        private void Search_GotFocus(object s, RoutedEventArgs e)  { if (SearchBox.Text.StartsWith("🔍")) SearchBox.Text = ""; SearchBox.Foreground = System.Windows.Media.Brushes.White; }
        private void Search_LostFocus(object s, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "🔍  Поиск..."; SearchBox.Foreground = System.Windows.Media.Brushes.Gray; } }
        private void Search_TextChanged(object s, TextChangedEventArgs e) => Apply(SearchBox.Text.StartsWith("🔍") ? "" : SearchBox.Text);

        private async void AddBtn_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddWindows.AddTeem { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null) return;
            var dlg = new AddWindows.AddTeem(item) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Delete_Click(object s, RoutedEventArgs e)
        {
            if ((int)((Button)s).Tag is int id && id > 0)
                if (MessageBox.Show("Удалить команду?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { await DatabaseService.DeleteTeamAsync(id); await Reload(); }
                    catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
                }
        }
    }
}
