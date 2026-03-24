using Katastata.Services;
using Katastata.UserControls;
using Katastata.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Katastata
{
    public partial class AuthWindow : Window
    {
        private readonly UserViewModel _vm;
        public int LoggedInUserId { get; private set; } = -1;

        public AuthWindow(ApiClient apiClient)
        {
            InitializeComponent();
            _vm = new UserViewModel(apiClient);
            _vm.LoginSuccessful += OnLoginSuccessful;
            ShowLoginPage(null, new RoutedEventArgs());
            DataContext = _vm;
        }

        private async void OnLoginSuccessful(int userId)
        {
            LoggedInUserId = userId;
            await PlaySuccessAnimationAsync();
            DialogResult = true;
            Close();
        }

        private async Task PlaySuccessAnimationAsync()
        {
            LoginButton.IsEnabled = false;
            RegisterButton.IsEnabled = false;
            SuccessOverlay.Visibility = Visibility.Visible;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            RootGrid.BeginAnimation(OpacityProperty, fadeOut);
            await Task.Delay(300);
            var fadeInOverlay = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700));
            var fadeInText = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(700));
            SuccessOverlay.BeginAnimation(OpacityProperty, fadeInOverlay);
            SuccessText.BeginAnimation(OpacityProperty, fadeInText);
            await Task.Delay(2000);
        }

        private void ShowLoginPage(object? sender, RoutedEventArgs e)
        {
            _vm.LoginMessage = string.Empty;
            ContentArea.Content = new LoginPage { DataContext = _vm };
            HighlightActiveButton(LoginButton);
        }

        private void ShowRegisterPage(object? sender, RoutedEventArgs e)
        {
            _vm.LoginMessage = string.Empty;
            ContentArea.Content = new RegisterPage { DataContext = _vm };
            HighlightActiveButton(RegisterButton);
        }

        private void CloseApp(object sender, RoutedEventArgs e) => Close();

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void HighlightActiveButton(System.Windows.Controls.Button activeButton)
        {
            var activeBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["AccentBrush"];
            var inactiveBrush = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["WindowBackgroundBrush"];
            var activeForeground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["AccentBrushActive"];
            var inactiveForeground = System.Windows.Media.Brushes.LightGray;

            LoginButton.Background = inactiveBrush;
            RegisterButton.Background = inactiveBrush;
            LoginButton.Foreground = inactiveForeground;
            RegisterButton.Foreground = inactiveForeground;
            activeButton.Background = activeBrush;
            activeButton.Foreground = activeForeground;
        }
    }
}
