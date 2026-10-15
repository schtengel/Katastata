using System.Security.Cryptography;
using System.Text;

namespace Katastata.Helpers
{
    public static class PasswordHelper
    {
        private const int Iterations = 600_000;
        private const string Prefix = "v2";

        // Формат: v2$итерации$соль(base64)$хэш(base64)
        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        // needsRehash = true, если пароль верный, но хранится в старом формате (SHA-256 без соли)
        public static bool Verify(string password, string stored, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(stored)) return false;

            if (stored.StartsWith(Prefix + "$"))
            {
                var parts = stored.Split('$');
                if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations)) return false;
                try
                {
                    byte[] salt = Convert.FromBase64String(parts[2]);
                    byte[] expected = Convert.FromBase64String(parts[3]);
                    byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // старый формат
            byte[] legacy = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            string legacyHex = Convert.ToHexString(legacy).ToLowerInvariant();
            bool ok = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(legacyHex), Encoding.ASCII.GetBytes(stored.ToLowerInvariant()));
            needsRehash = ok;
            return ok;
        }
    }
}