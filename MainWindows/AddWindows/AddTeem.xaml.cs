using System;
using System.Windows;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddTeem : Window
    {
        private readonly int _editId;

        // ── Add ──────────────────────────────────────────────────────
        public AddTeem()
        {
            InitializeComponent();
            _editId = -1;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddTeem(TeamItem item)
        {
            InitializeComponent();
            _editId          = item.Id;
            DialogTitle.Text = "Редактировать команду";
            SaveBtn.Content  = "Обновить";
            TxtName.Text     = item.Name;
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TxtName.Text))
            { ShowError("Введите название команды"); return; }

            SaveBtn.IsEnabled = false;
            try
            {
                if (_editId > 0)
                    await DatabaseService.UpdateTeamAsync(_editId, TxtName.Text.Trim());
                else
                    await DatabaseService.AddTeamAsync(TxtName.Text.Trim());
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
