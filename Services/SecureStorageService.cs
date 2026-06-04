using System.Security.Cryptography;
using System.Text;

namespace Spur.Services;

public sealed class SecureStorageService : ISecureStorageService
{
    private static readonly byte[] Entropy = "Spur.Launcher.v1"u8.ToArray();

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(cipherText);

            // Legacy plaintext fallback support
            var test = Encoding.UTF8.GetString(bytes);
            if (test.StartsWith("PLAIN:"))
                return test[6..];

            var decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}
