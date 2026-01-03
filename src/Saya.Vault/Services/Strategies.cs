using System.Security.Cryptography;
using System.Text;
using Saya.Vault.Models;

namespace Saya.Vault.Services;

public class plainTextStrategy : ICryptoStrategy
{
    public SecretType Type => SecretType.NonSensitive;

    public string Protect(string rawValue) => rawValue;

    public string Unprotect(string protectedValue) => protectedValue;
}

public class HashingStrategy : ICryptoStrategy
{
    public SecretType Type => SecretType.OneWay;

    public string Protect(string rawValue)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawValue);
        return Convert.ToBase64String(sha256.ComputeHash(bytes));
    }

    public string Unprotect(string protectedValue)
    {
        throw new InvalidOperationException("OneWay (Hash) veriler geri çözülemez!");
    }

   
}

 public class AesStrategy(string masterKey) : ICryptoStrategy
    {
        public SecretType Type => SecretType.Sensitive;

        public string Protect(string rawValue)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(masterKey);
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            ms.Write(aes.IV, 0, aes.IV.Length);

            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(rawValue);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public string Unprotect(string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue)) return string.Empty;
            var fullCipher = Convert.FromBase64String(protectedValue);
            if (fullCipher.Length <= 16) return string.Empty;
            using var aes = Aes.Create();
            var keyBytes = Encoding.UTF8.GetBytes(masterKey);
            Array.Resize(ref keyBytes, 32); 
            aes.Key = keyBytes;

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