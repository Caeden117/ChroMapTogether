using System.Security.Cryptography;

namespace ChroMapTogether.Providers
{
    public class SessionCodeProvider
    {
        private static readonly string _alphanumeric = "ABCDEFGHJKLMNPQRSTUVWXYZ12345789";

        public string Generate(int length = 5)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(length);
            return string.Create(length, randomBytes, (str, randomBytes) => {
                for (var i = 0; i < str.Length; i++)
                {
                    str[i] = _alphanumeric[randomBytes[i] % _alphanumeric.Length];
                }
            });
        }
    }
}
