using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddProject : Window
    {
        private readonly int _editId;

        // ── Add ──────────────────────────────────────────────────────
        public AddProject()
        {
            InitializeComponent();
            _editId = -1;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddProject(ProjectItem item)
        {
            InitializeComponent();
            _editId          = item.Id;
            DialogTitle.Text = "Редактировать проект";
            SaveBtn.Content  = "Обновить";
            TxtName.Text     = item.Name;
            TxtStart.Text    = item.StartDate == "—" ? "" : item.StartDate;
            TxtDeadline.Text = item.Deadline  == "—" ? "" : item.Deadline;
            foreach (ComboBoxItem ci in CmbStatus.Items)
                if (ci.Tag?.ToString() == item.Status) { CmbStatus.SelectedItem = ci; break; }
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
            { ShowError("Введите название проекта"); return; }
            if (!DateTime.TryParseExact(TxtStart.Text.Trim(), "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime start))
            { ShowError("Неверный формат начала. Используйте дд.мм.гггг"); return; }
            if (!DateTime.TryParseExact(TxtDeadline.Text.Trim(), "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime dl))
            { ShowError("Неверный формат дедлайна. Используйте дд.мм.гггг"); return; }

            var status = (CmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "active";
            SaveBtn.IsEnabled = false;
            try
            {
                if (_editId > 0)
                    await DatabaseService.UpdateProjectAsync(_editId, TxtName.Text.Trim(), start, dl, status);
                else
                    await DatabaseService.AddProjectAsync(TxtName.Text.Trim(),
                        TxtDesc.Text.Trim(), start, dl, status);
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
