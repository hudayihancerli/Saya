 
using Saya.Vault.Models;
using Saya.Vault.Services;

namespace Saya.Vault.Services;

public class CryptoFactory
{
    private readonly IEnumerable<ICryptoStrategy> _strategies;
    public CryptoFactory(IEnumerable<ICryptoStrategy> strategies)
    {
        _strategies = strategies;
    }

    public ICryptoStrategy GetStrategy(SecretType type)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Type == type);
        return strategy ?? throw new NotSupportedException($"Bu tip ({type}) için bir strateji tanımlanmamış!");
    }
}