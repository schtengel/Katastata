using Katastata.Contracts;
using Katastata.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows.Input;

namespace Katastata.ViewModels
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        public event Action<int>? LoginSuccessful;

        private string _loginMessage = string.Empty;
        public string LoginMessage
        {
            get => _loginMessage;
            set { _loginMessage = value; OnPropertyChanged(); }
        }

        public string RegisterUsername { get; set; } = string.Empty;
        public SecureString RegisterPassword { get; set; } = new SecureString();
        public SecureString RegisterPasswordConfirm { get; set; } = new SecureString();
        public string LoginUsername { get; set; } = string.Empty;
        public SecureString LoginPassword { get; set; } = new SecureString();

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public UserViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
            RegisterCommand = new RelayCommand(_ => RegisterUser());
            LoginCommand = new RelayCommand(_ => LoginUser());
        }

        private void RegisterUser()
        {
            LoginMessage = string.Empty;

            var passwordString = new System.Net.NetworkCredential(string.Empty, RegisterPassword).Password;
            var confirmString = new System.Net.NetworkCredential(string.Empty, RegisterPasswordConfirm).Password;

            if (string.IsNullOrWhiteSpace(RegisterUsername))
            {
                LoginMessage = "Введите имя пользователя.";
                return;
            }

            if (RegisterPassword.Length == 0)
            {
                LoginMessage = "Введите пароль.";
                return;
            }

            if (RegisterUsername.Length < 3)
            {
                LoginMessage = "Имя пользователя должно содержать минимум 3 символа.";
                return;
            }

            if (passwordString.Length < 5)
            {
                LoginMessage = "Пароль должен содержать минимум 5 символов.";
                return;
            }

            if (passwordString != confirmString)
            {
                LoginMessage = "Пароли не совпадают.";
                return;
            }

            var response = _apiClient.Register(new AuthRequest
            {
                Username = RegisterUsername,
                Password = passwordString,
                PcName = Environment.MachineName
            });

            if (!response.Success)
            {
                LoginMessage = response.Message;
                return;
            }

            LoginSuccessful?.Invoke(response.UserId);
        }

        private void LoginUser()
        {
            LoginMessage = string.Empty;

            var passwordString = new System.Net.NetworkCredential(string.Empty, LoginPassword).Password;

            if (string.IsNullOrWhiteSpace(LoginUsername))
            {
                LoginMessage = "Введите имя пользователя.";
                return;
            }

            if (LoginPassword.Length == 0)
            {
                LoginMessage = "Введите пароль.";
                return;
            }

            var response = _apiClient.Login(new AuthRequest
            {
                Username = LoginUsername,
                Password = passwordString
            });

            if (!response.Success)
            {
                LoginMessage = response.Message;
                return;
            }

            LoginSuccessful?.Invoke(response.UserId);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
