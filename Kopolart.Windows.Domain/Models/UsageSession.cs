using System;

namespace Kopolart.Windows.Domain.Models
{
    public class UsageSession
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Ip { get; set; } = "";
        public long DurationSeconds { get; set; } = 0;
    }
}