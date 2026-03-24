using Katastata.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Katastata.UserControls
{
    public partial class RegisterPage : System.Windows.Controls.UserControl
    {
        public RegisterPage()
        {
            InitializeComponent();

            Loaded += UserControl_Loaded;

            UsernameTextBox.Focus();
        }

        private void RegisterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.UserViewModel vm)
            {
                vm.RegisterPassword = ((PasswordBox)sender).SecurePassword;
            }
        }

        private void RegisterPasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserViewModel vm)
                vm.RegisterPasswordConfirm = (sender as PasswordBox)?.SecurePassword;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UsernameTextBox.Focus();
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}