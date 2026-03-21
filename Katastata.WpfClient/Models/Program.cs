
using System.Runtime.CompilerServices;

namespace Katastata.Models
{
    public class Program
    {
        public int Id { get; set; } //id программы
        public string Name { get; set; } //название программы
        public string Path { get; set; } //пусть к exe-файлу
        public int CategoryId { get; set; } //ссылка на категорию
        public Category Category { get; set; } //навигация к категории
        public ICollection<Session> Sessions { get; set; } //сессии этой программы
        public ICollection<Statistics> Statistics { get; set; } //статистика этой программы
    }
}
