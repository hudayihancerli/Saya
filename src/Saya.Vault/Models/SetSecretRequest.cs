namespace  Saya.Vault.Models;

public record SetSecretRequest(string Key, string Value, SecretType Type, DateTimeOffset? ExpiresAt);