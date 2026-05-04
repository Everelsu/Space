using System;
using System.Windows;
using System.Windows.Input;

namespace Space
{
    public partial class Autorisation : Window
    {
        public Autorisation()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var username = LoginBox.Text.Trim();
            var password = PasswordInput.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Заполните все поля");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход...";
            HideError();

            UserInfo user = null;
            try
            {
                user = await DatabaseService.LoginAsync(username, password);
            }
            catch (Exception ex)
            {
                ShowError("[DB] " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            if (user == null)
            {
                ShowError("Неверный логин или пароль");
                return;
            }

            try
            {
                new MainProject(user).Show();
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Войти";
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }

    }
}
