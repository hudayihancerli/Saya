using MessagePack;

namespace Saya.Vault.Models;

[MessagePackObject]
public class VaultItem
{
    [Key(0)]
    public string Value {get; set;} = string.Empty;
    [Key(1)]
    public SecretType Type {get; set;} 
    [Key(2)]
    public DateTimeOffset? ExpiresAt {get; set;}
    [Key(3)]
    public int AccessCount {get; set;}
    [Key(4)]
    public DateTimeOffset CreatedAt {get; set;} = DateTimeOffset.UtcNow;
    [Key(5)]
    public DateTimeOffset LastAccessedAt {get; set;}
 }