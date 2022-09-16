using System;

namespace ChroMapTogether.Models
{
    public class HttpResponseException : Exception
    {
        public int Status { get; set; } = 500;
        public object? Value { get; set; }

        public HttpResponseException() { }

        public HttpResponseException(int status)
        {
            Status = status;
        }

        public HttpResponseException(int status, object value)
        {
            Status = status;
            Value = value;
        }
    }
}
