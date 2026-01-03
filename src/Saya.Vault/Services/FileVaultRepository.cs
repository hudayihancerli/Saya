using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MessagePack;
using Saya.Vault.Models;

namespace Saya.Vault.Services;

public class FileVaultRepository : IVaultRepository
{

    private readonly ConcurrentDictionary<string, VaultItem> _memoryCache;
    private readonly string _filePath;
    private readonly byte[] _masterKeyBytes;
    private readonly object _fileLock = new();

    public FileVaultRepository(string masterKey, string filePath)
    {
        _filePath = filePath;
        _masterKeyBytes = Encoding.UTF8.GetBytes(masterKey.PadRight(32)[..32]);
        _memoryCache = new ConcurrentDictionary<string, VaultItem>();
        LoadFromFile();
    }

    public VaultItem? Get(string key)
    {
        if (!_memoryCache.TryGetValue(key, out var item))
        {
            return null;
        }

        if (item.ExpiresAt.HasValue && item.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            Remove(key);
            return null;
        }

        item.AccessCount++;
        item.LastAccessedAt = DateTimeOffset.UtcNow;
        return item;
    }

    public void Remove(string key)
    {
        _memoryCache.TryRemove(key, out _);
        SaveToFile();
    }

    public void Set(string key, VaultItem item)
    {
        _memoryCache[key] = item;
        SaveToFile();
    }

    private void SaveToFile()
    {
        lock (_fileLock)
        {
            var plainBytes = MessagePackSerializer.Serialize(_memoryCache);
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[16];
            var cipherBytes = new byte[plainBytes.Length];

            using var aes = new AesGcm(_masterKeyBytes, tag.Length);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            using var fileStream = new FileStream(_filePath, FileMode.Create);
            using var binaryWriter = new BinaryWriter(fileStream);

            binaryWriter.Write(nonce);
            binaryWriter.Write(tag);
            binaryWriter.Write(cipherBytes);
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            byte[] plainBytes;

            lock (_fileLock)
            {
                using var fileStream = new FileStream(_filePath, FileMode.Open);
                using var binaryReader = new BinaryReader(fileStream);

                if (fileStream.Length < 28)
                {
                    throw new Exception("Dosya boyutu geçersiz");
                }

                var nonce = binaryReader.ReadBytes(12);
                var tag = binaryReader.ReadBytes(16);
                var cipherBytes = binaryReader.ReadBytes((int)fileStream.Length - 28);

                plainBytes = new byte[cipherBytes.Length];

                using var aes = new AesGcm(_masterKeyBytes, tag.Length);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            var data = MessagePackSerializer.Deserialize<Dictionary<string, VaultItem>>(plainBytes);
            if (data is null)
            {
                return;
            }
            foreach (var kvp in data)
            {
                _memoryCache[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"dosyası bozuk veya şifre yanlış! ({ex.Message})");
            HandleCorruptFile();
        }

    }

    private void HandleCorruptFile()
    {
        var backupName = $"{_filePath}.{DateTimeOffset.UtcNow:yyyyyMMddHHmmss}.corrupted";
        try
        {
            File.Move(_filePath, backupName);
            Console.WriteLine($"Bozuk dosya şuraya taşındı: {backupName}");
        }
        catch
        {
            File.Delete(_filePath);
        }
        _memoryCache.Clear();
    }

}