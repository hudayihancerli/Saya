using Microsoft.Extensions.Options;
using Saya.Vault.Endpoints;
using Saya.Vault.Services;
using static Saya.Vault.Services.HashingStrategy;

var builder = WebApplication.CreateBuilder(args);

var masterKey = 
    Environment.GetEnvironmentVariable("SAYA_MASTER_KEY")
    ?? throw new Exception("master key bulunamadı");

var storagePath = Environment.GetEnvironmentVariable("SAYA_STORAGE_PATH") 
    ?? "saya_vault.bindb";

builder.Services.AddSingleton<ICryptoStrategy, plainTextStrategy>();
builder.Services.AddSingleton<ICryptoStrategy, HashingStrategy>();
builder.Services.AddSingleton<ICryptoStrategy>(sp => new AesStrategy(masterKey));


builder.Services.AddSingleton<IVaultRepository>(sp => new FileVaultRepository(masterKey, storagePath));
builder.Services.AddSingleton<CryptoFactory>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // JSON dosyasını oluşturur
    app.UseSwagger();
    // O klasik mavi/yeşil arayüzü açar
    app.UseSwaggerUI(); 
}

app.MapVaultRoutes();

app.Run();
