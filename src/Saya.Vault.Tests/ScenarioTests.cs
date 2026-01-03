using FluentAssertions;
using Saya.Vault.Models;
using Saya.Vault.Services;
using Xunit;

namespace Saya.Vault.Tests;

public class ScenarioTests
{
    [Fact]
    public async Task Full_Vault_Flow_Scenario()
    {
        var file = Path.GetTempFileName();
        var key = "12345678901234567890123456789012";
        
        var factory = new CryptoFactory(new ICryptoStrategy[] { new AesStrategy(key) });
        var repo = new FileVaultRepository(key, file);

        var secretData = "BankaSifresi";
        var encrypted = factory.GetStrategy(SecretType.Sensitive).Protect(secretData);
        
        repo.Set("BankDB", new VaultItem 
        { 
            Value = encrypted, 
            Type = SecretType.Sensitive 
        });

        repo.Set("Token", new VaultItem 
        { 
            Value = "GeciciToken", 
            Type = SecretType.NonSensitive,
            ExpiresAt = DateTime.UtcNow.AddSeconds(1) 
        });

        await Task.Delay(1500);

        var newRepo = new FileVaultRepository(key, file);

        var bankItem = newRepo.Get("BankDB");
        bankItem.Should().NotBeNull();
        
        var decrypted = factory.GetStrategy(SecretType.Sensitive).Unprotect(bankItem!.Value);
        decrypted.Should().Be("BankaSifresi");

        var tokenItem = newRepo.Get("Token");
        tokenItem.Should().BeNull("Süresi dolan veri otomatik silinmeliydi");

        File.Delete(file);
    }
}