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
        private readonly UserInfo _user;

        public ReportWindows() : this(null) { }

        public ReportWindows(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => { ApplyRole(); await Reload(); };
        }

        // admin + manager can edit / delete reports; everyone can add
        private bool CanManage => _user?.Role == "admin" || _user?.Role == "manager";

        private void ApplyRole()
        {
            if (!CanManage)
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

        private async void AddBtn_Click(object s, RoutedEventArgs e)
        {
            var dlg = new AddWindows.AddReport { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
        }

        private async void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(w => w.Id == id);
            if (item == null) return;
            var dlg = new AddWindows.AddReport(item) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await Reload();
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
