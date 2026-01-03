using System.Security.Cryptography;
using System.Text;

namespace Saya.Vault.Services;

public static class CryptoEngine
{
    private static readonly string MasterKey = GetEnvironmentKey();

    private static string GetEnvironmentKey()
    {
        var key = Environment.GetEnvironmentVariable("SAYA_MASTER_KEY");

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Key anahtarını bulamadım.");
        }

        if (key.Length is not 32)
        {
            throw new InvalidOperationException("Key yeterince uzun değil :)");
        }

        return key;
    }

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(MasterKey);
        aes.GenerateIV();

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        ms.Write(aes.IV, 0, aes.IV.Length);

        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        sw.Write(plainText);
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(MasterKey);

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}