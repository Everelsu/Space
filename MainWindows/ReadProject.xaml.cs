using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Space
{
    public partial class ReadProject : Window
    {
        public ReadProject()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Close_Click(object s, RoutedEventArgs e) => Close();

        private void GitHub_Click(object s, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo("https://github.com/Everelsu/Space") { UseShellExecute = true });
    }
}
