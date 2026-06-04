namespace Spur.Services;

public interface ISecureStorageService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
