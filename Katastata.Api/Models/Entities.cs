namespace Katastata.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PCName { get; set; }
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Statistics> Statistics { get; set; } = new List<Statistics>();
    }

    public class ProgramEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        public ICollection<Statistics> Statistics { get; set; } = new List<Statistics>();
    }

    public class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ProgramId { get; set; }
        public ProgramEntity? Program { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class Statistics
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int ProgramId { get; set; }
        public ProgramEntity? Program { get; set; }
        public TimeSpan TotalTime { get; set; }
        public DateTime? LastLaunch { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<ProgramEntity>? Programs { get; set; }
    }
}
