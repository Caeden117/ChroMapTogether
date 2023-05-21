namespace ChroMapTogether.Models.Responses
{
    public class JoinSessionResponse
    {
        public string ip { get; set; } = string.Empty;
        public int port { get; set; }
        public string appVersion { get; set; } = string.Empty;
    }
}
