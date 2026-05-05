using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddWorker : Window
    {
        private readonly int  _editId;
        private readonly int? _editTeamId;

        // ── Add ──────────────────────────────────────────────────────
        public AddWorker()
        {
            InitializeComponent();
            _editId     = -1;
            _editTeamId = null;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddWorker(EmployeeItem item)
        {
            InitializeComponent();
            _editId          = item.Id;
            _editTeamId      = item.TeamId;
            DialogTitle.Text = "Редактировать сотрудника";
            SaveBtn.Content  = "Обновить";
            TxtFullName.Text = item.FullName;
            TxtEmail.Text    = item.Email    == "—" ? "" : item.Email;
            TxtPosition.Text = item.Position == "—" ? "" : item.Position;
            foreach (ComboBoxItem ci in CmbRole.Items)
                if (ci.Tag?.ToString() == item.Role) { CmbRole.SelectedItem = ci; break; }
            // Hide login section when editing
            LoginSection.Visibility = Visibility.Collapsed;
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            var teams = await DatabaseService.GetTeamsDropdownAsync();
            // Prepend empty item so user can leave team unassigned
            teams.Insert(0, new DropdownItem { Id = 0, Name = "— Без команды —" });
            CmbTeam.ItemsSource = teams;

            if (_editTeamId.HasValue)
            {
                foreach (DropdownItem di in CmbTeam.Items)
                    if (di.Id == _editTeamId.Value) { CmbTeam.SelectedItem = di; break; }
            }
            else
            {
                CmbTeam.SelectedIndex = 0;   // "— Без команды —"
            }
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TxtFullName.Text))
            { ShowError("Введите полное имя сотрудника"); return; }

            var role   = (CmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "developer";
            var teamDi = CmbTeam.SelectedItem as DropdownItem;
            int? teamId = (teamDi != null && teamDi.Id > 0) ? teamDi.Id : (int?)null;

            SaveBtn.IsEnabled = false;
            try
            {
                if (_editId > 0)
                {
                    await DatabaseService.UpdateEmployeeAsync(_editId,
                        TxtFullName.Text.Trim(), TxtEmail.Text.Trim(),
                        TxtPosition.Text.Trim(), role, teamId);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(TxtUsername.Text) ||
                        string.IsNullOrWhiteSpace(TxtPassword.Text))
                    { ShowError("Заполните логин и пароль"); SaveBtn.IsEnabled = true; return; }

                    await DatabaseService.AddEmployeeAsync(
                        TxtFullName.Text.Trim(), TxtEmail.Text.Trim(),
                        TxtPosition.Text.Trim(), TxtUsername.Text.Trim(),
                        TxtPassword.Text.Trim(), role, teamId);
                }
                DialogResult = true;
            }
            catch (Exception ex) { ShowError(ex.Message); SaveBtn.IsEnabled = true; }
        }

        private void ShowError(string msg)
        {
            ErrorText.Text       = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
