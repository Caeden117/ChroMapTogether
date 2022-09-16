using System.Security.Cryptography;

namespace ChroMapTogether.Providers
{
    public class ServerCodeProvider
    {
        private static readonly string _alphanumeric = "ABCDEFGHJKLMNPQRSTUVWXYZ012345789";

        private readonly RNGCryptoServiceProvider _rngCryptoServiceProvider;

        public ServerCodeProvider(RNGCryptoServiceProvider rngCryptoServiceProvider)
        {
            _rngCryptoServiceProvider = rngCryptoServiceProvider;
        }

        public string Generate(int length = 5)
        {
            var randomBytes = new byte[length];
            _rngCryptoServiceProvider.GetBytes(randomBytes);
            return string.Create(length, randomBytes, (str, randomBytes) => {
                for (var i = 0; i < str.Length; i++)
                {
                    str[i] = _alphanumeric[randomBytes[i] % _alphanumeric.Length];
                }
            });
        }
    }
}
