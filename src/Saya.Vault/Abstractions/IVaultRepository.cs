using Microsoft.AspNetCore.DataProtection;
using Saya.Vault.Models;

namespace Saya.Vault.Services;

public interface ICryptoStrategy
{
    string Protect(string rawValue);
    string Unprotect(string protectedValue);
    SecretType Type {get;}
}

public interface IVaultRepository
{
    void Set(string key, VaultItem item);
    VaultItem? Get(string key);
    void Remove(string key);
}

