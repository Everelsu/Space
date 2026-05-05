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
        private int _editId = -1;

        public TeemProject()
        {
            InitializeComponent();
            Loaded += async (s, e) => await Reload();
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

        private void AddBtn_Click(object s, RoutedEventArgs e)
        {
            _editId = -1;
            FormTitle.Text  = "Новая команда";
            SaveBtn.Content = "Сохранить";
            AddName.Text    = "";
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(t => t.Id == id);
            if (item == null) return;
            _editId = id;
            FormTitle.Text  = "Редактировать команду";
            SaveBtn.Content = "Обновить";
            AddName.Text    = item.Name;
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = Visibility.Visible;
        }

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddName.Text)) { AddError.Text = "Введите название"; AddError.Visibility = Visibility.Visible; return; }
            try
            {
                if (_editId > 0)
                    await DatabaseService.UpdateTeamAsync(_editId, AddName.Text.Trim());
                else
                    await DatabaseService.AddTeamAsync(AddName.Text.Trim());
                _editId = -1;
                AddName.Text = "";
                AddError.Visibility = Visibility.Collapsed;
                AddPanel.Visibility = Visibility.Collapsed;
                await Reload();
            }
            catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
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
