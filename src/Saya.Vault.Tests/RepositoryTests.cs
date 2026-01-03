using FluentAssertions;
using Saya.Vault.Models;
using Saya.Vault.Services;

namespace Saya.Vault.Tests;

public class RepositoryTests : IDisposable
{

    private const string MasterKey = "TEST_KEY_123456789012";
    private readonly string _tempFile;

    public RepositoryTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_vault_{Guid.NewGuid()}.bin");
    }


    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        foreach (var f in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileName(_tempFile)}*.corrupted"))
        {
            File.Delete(f);
        }
    }

    [Fact]
    // Metodun Amacı: Set metodu çağrıldığında veriyi diske yazmalı ve uygulama yeniden başlatıldığında (yeni repo instance) veriyi korumalıdır.
    public void Should_Save_And_Load_From_Disk()
    {
        var repo1 = new FileVaultRepository(MasterKey, _tempFile);
        var item = new VaultItem { Value = "TestValue", Type = SecretType.NonSensitive };
        repo1.Set("MyKey", item);

        File.Exists(_tempFile).Should().BeTrue();

        var repo2 = new FileVaultRepository(MasterKey, _tempFile);
        var loadedItem = repo2.Get("MyKey");

        loadedItem.Should().NotBeNull();
        loadedItem!.Value.Should().Be("TestValue");
    }

    [Fact]
    // Metodun Amacı: Dosya dışarıdan bozulduğunda (byte manipülasyonu), sistem bunu algılamalı, hafızayı sıfırlamalı ve bozuk dosyayı yedeklemelidir.
    public void Should_Handle_Corrupted_File_Gracefully()
    {
        var repo = new FileVaultRepository(MasterKey, _tempFile);
        repo.Set("Key1", new VaultItem { Value = "Data" });

        var bytes = File.ReadAllBytes(_tempFile);
        bytes[bytes.Length / 2] = (byte)(bytes[bytes.Length / 2] + 1); 
        File.WriteAllBytes(_tempFile, bytes);

        var repoRecovered = new FileVaultRepository(MasterKey, _tempFile);
        
        repoRecovered.Get("Key1").Should().BeNull();

        var corruptedFiles = Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileName(_tempFile)}*.corrupted");
        corruptedFiles.Should().HaveCountGreaterThan(0);
    }
}