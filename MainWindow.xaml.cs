using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Space
{
    public partial class MainWindow : Window
    {
        private readonly UserInfo _user;
        private Button _activeBtn;

        public MainWindow(UserInfo user)
        {
            InitializeComponent();
            _user = user;

            SidebarRole.Text     = (_user.Role ?? "user").ToUpper();
            SidebarUsername.Text = _user.Username;

            // Hide admin-only nav sections for non-admins
            bool isAdmin = _user.Role == "admin";
            BtnEmployees.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnTeams.Visibility     = isAdmin ? Visibility.Visible : Visibility.Collapsed;

            Navigate(new MainProject(_user), BtnProjects);
        }

        private void Navigate(UserControl page, Button btn)
        {
            if (_activeBtn != null)
                _activeBtn.Style = (Style)FindResource("NavBtnStyle");
            btn.Style    = (Style)FindResource("NavBtnActiveStyle");
            _activeBtn   = btn;
            PageHost.Content = page;
        }

        private void NavProjects_Click(object s, RoutedEventArgs e)  => Navigate(new MainProject(_user),        BtnProjects);
        private void NavEmployees_Click(object s, RoutedEventArgs e) => Navigate(new TasksWindow(_user),        BtnEmployees);
        private void NavTeams_Click(object s, RoutedEventArgs e)     => Navigate(new TeemProject(_user),        BtnTeams);
        private void NavTasks_Click(object s, RoutedEventArgs e)     => Navigate(new TaskManageWindow(_user),   BtnTasks);
        private void NavReports_Click(object s, RoutedEventArgs e)   => Navigate(new ReportWindows(_user),      BtnReports);

        private void Exit_Click(object s, RoutedEventArgs e) { new Autorisation().Show(); Close(); }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Minimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object s, RoutedEventArgs e)    => Close();
    }
}
