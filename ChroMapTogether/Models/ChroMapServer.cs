using System;

namespace ChroMapTogether.Models
{
    public class ChroMapServer
    {
        public Guid guid { get; set; }
        public string ip { get; set; } = string.Empty;
        public int port { get; set; }
        public string code { get; set; } = string.Empty;
        public DateTime expiry { get; set; } = DateTime.MaxValue;
    }
}
