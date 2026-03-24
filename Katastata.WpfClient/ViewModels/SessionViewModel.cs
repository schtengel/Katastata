using Katastata.Models;
using System;

namespace Katastata.ViewModels
{
    public class SessionViewModel
    {
        public string ProgramName { get; set; }
        public string CategoryName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public SessionViewModel(Session session)
        {
            ProgramName = session.Program?.Name ?? "Unknown";
            CategoryName = session.Program?.Category?.Name ?? "Не классифицировано";
            StartTime = session.StartTime;
            EndTime = session.EndTime;
        }
    }
}
