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
        private int _editId = -1;
        private readonly UserInfo _user;

        public TasksWindow() : this(null) { }

        public TasksWindow(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Loaded += async (s, e) => { ApplyRole(); await Reload(); };
        }

        private bool IsAdmin => _user?.Role == "admin";

        private void ApplyRole()
        {
            // Non-admins can't navigate here, but guard anyway
            if (!IsAdmin)
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

        private void AddBtn_Click(object s, RoutedEventArgs e)
        {
            _editId = -1;
            FormTitle.Text  = "Новый работник";
            SaveBtn.Content = "Сохранить";
            AddFullName.Text = AddEmail.Text = AddPosition.Text = AddUsername.Text = AddPassword.Text = "";
            AddRole.SelectedIndex = 0;
            LoginRow.Visibility   = Visibility.Visible;
            AddUsername.IsEnabled = AddPassword.IsEnabled = true;
            AddError.Visibility   = Visibility.Collapsed;
            AddPanel.Visibility   = AddPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Edit_Click(object s, RoutedEventArgs e)
        {
            var id   = (int)((Button)s).Tag;
            var item = _all.FirstOrDefault(p => p.Id == id);
            if (item == null) return;

            _editId = id;
            FormTitle.Text  = "Редактировать работника";
            SaveBtn.Content = "Обновить";
            AddFullName.Text = item.FullName;
            AddEmail.Text    = item.Email    == "—" ? "" : item.Email;
            AddPosition.Text = item.Position == "—" ? "" : item.Position;
            foreach (ComboBoxItem ci in AddRole.Items)
                if (ci.Tag?.ToString() == item.Role) { AddRole.SelectedItem = ci; break; }
            LoginRow.Visibility = Visibility.Collapsed;
            AddError.Visibility = Visibility.Collapsed;
            AddPanel.Visibility = Visibility.Visible;
        }

        private async void SaveAdd_Click(object s, RoutedEventArgs e)
        {
            if (_editId > 0)
            {
                if (string.IsNullOrWhiteSpace(AddFullName.Text))
                { AddError.Text = "Введите имя"; AddError.Visibility = Visibility.Visible; return; }
                try
                {
                    var role = (AddRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "developer";
                    await DatabaseService.UpdateEmployeeAsync(_editId, AddFullName.Text.Trim(),
                        AddEmail.Text.Trim(), AddPosition.Text.Trim(), role);
                    _editId = -1;
                    AddPanel.Visibility = Visibility.Collapsed;
                    AddError.Visibility = Visibility.Collapsed;
                    await Reload();
                }
                catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(AddFullName.Text) || string.IsNullOrWhiteSpace(AddUsername.Text) || string.IsNullOrWhiteSpace(AddPassword.Text))
                { AddError.Text = "Заполните Имя, Логин и Пароль"; AddError.Visibility = Visibility.Visible; return; }
                try
                {
                    var role = (AddRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "developer";
                    await DatabaseService.AddEmployeeAsync(AddFullName.Text.Trim(), AddEmail.Text.Trim(),
                        AddPosition.Text.Trim(), AddUsername.Text.Trim(), AddPassword.Text.Trim(), role);
                    AddFullName.Text = AddEmail.Text = AddPosition.Text = AddUsername.Text = AddPassword.Text = "";
                    AddError.Visibility = Visibility.Collapsed;
                    AddPanel.Visibility = Visibility.Collapsed;
                    await Reload();
                }
                catch (Exception ex) { AddError.Text = ex.Message; AddError.Visibility = Visibility.Visible; }
            }
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
