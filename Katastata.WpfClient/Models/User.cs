using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katastata.Models
{
    public class User
    {
        public int Id { get; set; } //id пользователя
        public string Username { get; set; } //имя пользователя
        public string PasswordHash { get; set; } //хеш пароля
        public string? PCName { get; set; } //имя компьютера
        public ICollection<Session> Sessions { get; set; } //сессии пользователя
        public ICollection<Statistics> Statistics { get; set; } //статистика этого пользователя
    }
}
