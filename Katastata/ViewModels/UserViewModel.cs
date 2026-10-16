using Katastata.Data;
using Katastata.Helpers;
using Katastata.Models;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows;
using System.Windows.Input;

namespace Katastata.ViewModels
{
    public class UserViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        public event Action<int> LoginSuccessful;

        private string _loginMessage;
        public string LoginMessage
        {
            get => _loginMessage;
            set { _loginMessage = value; OnPropertyChanged(); }
        }

        public string RegisterUsername { get; set; }
        public SecureString RegisterPassword { get; set; } = new SecureString();
        public SecureString RegisterPasswordConfirm { get; set; } = new SecureString();
        public string LoginUsername { get; set; }
        public SecureString LoginPassword { get; set; } = new SecureString();

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }

        public UserViewModel() { }

        public UserViewModel(AppDbContext db)
        {
            _db = db;
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

            if (_db.Users.Any(u => u.Username == RegisterUsername))
            {
                LoginMessage = "Такой пользователь уже существует.";
                return;
            }

            var user = new User
            {
                Username = RegisterUsername,
                PasswordHash = PasswordHelper.HashPassword(passwordString),
                PCName = Environment.MachineName
            };
            _db.Users.Add(user);
            _db.SaveChanges();

            LoginSuccessful?.Invoke(user.Id);
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

            var user = _db.Users.FirstOrDefault(u => u.Username == LoginUsername);

            if (user == null || !PasswordHelper.Verify(passwordString, user.PasswordHash, out bool needsRehash))
            {
                LoginMessage = "Неверный логин или пароль.";
                return;
            }

            if (needsRehash)
            {
                user.PasswordHash = PasswordHelper.HashPassword(passwordString);
                _db.SaveChanges();
            }

            LoginSuccessful?.Invoke(user.Id);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
