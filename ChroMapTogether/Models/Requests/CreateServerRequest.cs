namespace ChroMapTogether.Models.Requests
{
    public class CreateServerRequest
    {
        public string? ip { get; set; } = null;
        public int port { get; set; }
    }
}
