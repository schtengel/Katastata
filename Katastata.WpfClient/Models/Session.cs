using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katastata.Models
{
    public class Session
    {
        public int Id { get; set; } //id сессии
        public int UserId { get; set; } //пользователь сессии
        public User User { get; set; } //навигация к пользователю
        public int ProgramId { get; set; } //программа в сессии
        public Program Program { get; set; } //навигация к программе
        public DateTime StartTime { get; set; } //время начала сессии
        public DateTime EndTime { get; set; } //время конца сессии
    }
}
