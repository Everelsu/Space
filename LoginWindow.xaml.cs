using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace Space
{
    public partial class LoginWindow : FluentWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameInput.Text.Trim();
            var password = PasswordInput.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Заполните все поля");
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "Вход...";
            HideError();

            try
            {
                var user = await DatabaseService.LoginAsync(username, password);

                if (user != null)
                {
                    new MainWindow(user).Show();
                    Close();
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка подключения: " + ex.Message);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "Войти";
            }
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
