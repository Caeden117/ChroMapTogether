using Serilog.Events;
using System.Collections.Generic;

namespace ChroMapTogether.Configuration
{
    public class SerilogConfiguration
    {
        public class MinimumLevelConfiguration
        {
            public LogEventLevel Default { get; set; } = LogEventLevel.Information;
            public Dictionary<string, LogEventLevel> Overrides { get; set; } = new();
        }

        public class FileConfiguration
        {
            public string? Path { get; set; }
            public long FileSizeLimitBytes { get; set; } = 1073741824;
            public int RetainedFileCountLimit { get; set; } = 31;
            public bool Buffered { get; set; } = false;
        }

        public MinimumLevelConfiguration MinimumLevel { get; set; } = new();
        public bool WriteToConsole { get; set; } = true;
        public FileConfiguration? File { get; set; }
    }
}
