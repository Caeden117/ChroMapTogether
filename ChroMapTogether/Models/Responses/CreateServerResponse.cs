using System;

namespace ChroMapTogether.Models.Responses
{
    public class CreateServerResponse
    {
        public Guid guid { get; set; }
        public string ip { get; set; } = string.Empty;
        public int port { get; set; }
        public string code { get; set; } = string.Empty;
    }
}
