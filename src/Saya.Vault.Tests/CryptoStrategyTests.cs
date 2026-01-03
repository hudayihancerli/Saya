using FluentAssertions;
using Saya.Vault.Models;
using Saya.Vault.Services;
using static Saya.Vault.Services.HashingStrategy;

namespace Saya.Vault.Tests;

public class CryptoStrategyTests
{
    private const string MasterKey = "12345678901234567890123456789012";


    [Fact]
    //Şifrele ve Çöz işlemi (RoundTrip) sonunda elime orijinal veri geçmeli.
    public void AesStrategy_Should_Encrypt_And_Decrypt_Correctly()
    {
        var strategy = new AesStrategy(MasterKey);
        var originalValue = "CokGizliBirSifre!";

        var encrypted = strategy.Protect(originalValue);
        var decrypted = strategy.Unprotect(encrypted);

        encrypted.Should().NotBe(originalValue);
        decrypted.Should().Be(originalValue);
        strategy.Type.Should().Be(SecretType.Sensitive);
    }

    [Fact]
    //Hash işlemi deterministiktir (aynı girdi = aynı çıktı) ve geri döndürülemez (OneWay).
    public void HashingStrategy_Should_Be_OneWay()
    {
        var strategy = new HashingStrategy();
        var password = "AdminPassword123";

        var hash1 = strategy.Protect(password);
        var hash2 = strategy.Protect(password);

        hash1.Should().Be(hash2);
        hash1.Should().NotBe(password);

        Action act = () => strategy.Unprotect(hash1);
        act.Should().Throw<InvalidOperationException>()
        .WithMessage("*OneWay*");
    }


}