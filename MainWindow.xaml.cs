using System.Windows;

namespace Space
{
    public partial class MainWindow : Window
    {
        private readonly UserInfo _user;

        public MainWindow(UserInfo user)
        {
            InitializeComponent();
            _user = user;
            Title = $"IT Project Manager — {user.Username} ({user.Role})";
        }
    }
}
