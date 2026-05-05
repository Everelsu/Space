using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddWorker : Window
    {
        private readonly int _editId;

        // ── Add ──────────────────────────────────────────────────────
        public AddWorker()
        {
            InitializeComponent();
            _editId = -1;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddWorker(EmployeeItem item)
        {
            InitializeComponent();
            _editId          = item.Id;
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

            var role = (CmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "developer";
            SaveBtn.IsEnabled = false;
            try
            {
                if (_editId > 0)
                {
                    await DatabaseService.UpdateEmployeeAsync(_editId,
                        TxtFullName.Text.Trim(), TxtEmail.Text.Trim(),
                        TxtPosition.Text.Trim(), role);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(TxtUsername.Text) ||
                        string.IsNullOrWhiteSpace(TxtPassword.Text))
                    { ShowError("Заполните логин и пароль"); SaveBtn.IsEnabled = true; return; }

                    await DatabaseService.AddEmployeeAsync(
                        TxtFullName.Text.Trim(), TxtEmail.Text.Trim(),
                        TxtPosition.Text.Trim(), TxtUsername.Text.Trim(),
                        TxtPassword.Text.Trim(), role);
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
