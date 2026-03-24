using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katastata.Models
{
    public class Statistics
    {
        public int Id { get; set; } //id статистики
        public int UserId { get; set; } //пользователь статистики
        public User User { get; set; } //навигация к пользователю
        public int ProgramId { get; set; } //программа статистики
        public Program Program { get; set; } //навигация к статистике
        public TimeSpan TotalTime { get; set; } //общее время
        public DateTime? LastLaunch {  get; set; } //последнее время запуска

    }
}
