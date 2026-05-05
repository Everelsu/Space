using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space.AddWindows
{
    public partial class AddTask : Window
    {
        private readonly TaskManageItem _editItem;

        // ── Add ──────────────────────────────────────────────────────
        public AddTask()
        {
            InitializeComponent();
            _editItem = null;
        }

        // ── Edit ─────────────────────────────────────────────────────
        public AddTask(TaskManageItem item)
        {
            InitializeComponent();
            _editItem = item;
        }

        private async void Window_Loaded(object s, RoutedEventArgs e)
        {
            // Load dropdowns
            var projects  = await DatabaseService.GetProjectsDropdownAsync();
            var employees = await DatabaseService.GetEmployeesDropdownAsync();
            CmbProject.ItemsSource  = projects;
            CmbAssignee.ItemsSource = employees;

            if (_editItem != null)
            {
                DialogTitle.Text = "Редактировать задачу";
                SaveBtn.Content  = "Обновить";
                TxtTitle.Text    = _editItem.Title;
                TxtDeadline.Text = _editItem.Deadline == "—" ? "" : _editItem.Deadline;

                foreach (DropdownItem di in CmbProject.Items)
                    if (di.Id == _editItem.ProjectId) { CmbProject.SelectedItem = di; break; }
                foreach (DropdownItem di in CmbAssignee.Items)
                    if (di.Id == _editItem.AssigneeId) { CmbAssignee.SelectedItem = di; break; }

                SetCombo(CmbPriority, _editItem.Priority);
                SetCombo(CmbStatus,   _editItem.Status);
            }
        }

        private void SetCombo(ComboBox cb, string value)
        {
            foreach (ComboBoxItem ci in cb.Items)
                if (ci.Tag?.ToString() == value) { cb.SelectedItem = ci; return; }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;

        private async void Save_Click(object s, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TxtTitle.Text))
            { ShowError("Введите название задачи"); return; }

            var proj = CmbProject.SelectedItem as DropdownItem;
            if (proj == null)
            { ShowError("Выберите проект"); return; }

            var priority = (CmbPriority.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "low";
            var status   = (CmbStatus.SelectedItem   as ComboBoxItem)?.Tag?.ToString() ?? "open";
            var assignee = CmbAssignee.SelectedItem as DropdownItem;
            DateTime? deadline = null;

            if (!string.IsNullOrWhiteSpace(TxtDeadline.Text))
            {
                if (!DateTime.TryParseExact(TxtDeadline.Text.Trim(), "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dl))
                { ShowError("Неверный формат дедлайна. Используйте дд.мм.гггг"); return; }
                deadline = dl;
            }

            SaveBtn.IsEnabled = false;
            try
            {
                if (_editItem != null)
                    await DatabaseService.UpdateTaskManageAsync(_editItem.Id, TxtTitle.Text.Trim(),
                        proj.Id, assignee?.Id, priority, status, deadline);
                else
                    await DatabaseService.AddTaskManageAsync(TxtTitle.Text.Trim(),
                        proj.Id, assignee?.Id, priority, status, deadline);
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
