using Katastata.Models;
using System;

namespace Katastata.ViewModels
{
    public class StatisticsViewModelItem
    {
        public string ProgramName { get; set; }
        public string TotalTime { get; set; }
        public string LastLaunch { get; set; }

        public StatisticsViewModelItem(Statistics stat)
        {
            ProgramName = stat.Program?.Name ?? "Unknown";
            TotalTime = FormatTimeSpan(stat.TotalTime);
            LastLaunch = stat.LastLaunch?.ToString("g") ?? "-";
        }

        private string FormatTimeSpan(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}ч {time.Minutes}м";
            else if (time.TotalMinutes >= 1)
                return $"{time.Minutes}м {time.Seconds}с";
            else
                return $"{time.Seconds}с";
        }
    }
}
