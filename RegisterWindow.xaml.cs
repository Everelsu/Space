using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Space
{
    public partial class RegisterWindow : FluentWindow
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var fullName  = FullNameBox.Text.Trim();
            var username  = UsernameBox.Text.Trim();
            var password  = PasswordBox.Password;
            var confirm   = ConfirmPasswordBox.Password;
            var role      = ((ComboBoxItem)RoleBox.SelectedItem).Tag.ToString();

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
            {
                ShowError("Заполните все поля");
                return;
            }

            if (password != confirm)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен быть не менее 4 символов");
                return;
            }

            RegisterButton.IsEnabled = false;
            RegisterButton.Content = "Регистрация...";
            HideError();

            try
            {
                var ok = await DatabaseService.RegisterAsync(username, password, fullName, role);

                if (ok)
                {
                    SuccessText.Text = "Аккаунт создан! Можете войти.";
                    SuccessText.Visibility = Visibility.Visible;
                    RegisterButton.Content = "Готово";

                    await System.Threading.Tasks.Task.Delay(1500);
                    Close();
                }
                else
                {
                    ShowError("Логин уже занят");
                    RegisterButton.IsEnabled = true;
                    RegisterButton.Content = "Зарегистрироваться";
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка: " + ex.Message);
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "Зарегистрироваться";
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
