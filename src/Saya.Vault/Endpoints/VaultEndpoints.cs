using Microsoft.AspNetCore.Mvc;
using Saya.Vault.Models;
using Saya.Vault.Services;

namespace Saya.Vault.Endpoints;

public static class VaultEndpoints
{
    public static void MapVaultRoutes(this WebApplication app)
    {
        app.MapPost("/set", async (
            [FromBody] SetSecretRequest request,
            [FromServices] CryptoFactory factory,
            [FromServices] IVaultRepository repo) =>
        {
            var strategy = factory.GetStrategy(request.Type);
            var protectedValue = strategy.Protect(request.Value);

            var item = new VaultItem
            {
                Value = protectedValue,
                Type = request.Type,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTimeOffset.UtcNow
            };

            repo.Set(request.Key, item);

            return Results.Ok(new
            {
                Message = "Kaydedildi",
                Mode = strategy.GetType().Name
            });
        });


        app.MapGet("/get/{key}", async (
            string key,
            HttpContext context,
            [FromServices] CryptoFactory factory,
            [FromServices] IVaultRepository repository
        ) =>
        {
            if (context.Request.Headers["X-Vault-Token"] != "SAYA_INTERNAL_ACCESS")
            {
                return Results.Unauthorized();
            }
            var item = repository.Get(key);
            if (item is null)
            {
                return Results.NotFound();
            }

            var strategy = factory.GetStrategy(item.Type);

            try
            {
                var plainValue = strategy.Unprotect(item.Value);
                return Results.Ok(new
                {
                    Key = key,
                    Value = plainValue,
                    Stast = new
                    {
                        item.AccessCount, 
                        item.LastAccessedAt
                    }
                });
            }
            catch (InvalidOperationException)
            {
                return Results.Ok(new
                {
                    Key = key,
                    Value = item.Value,
                    Message = "OneWay (Hash) veri çözülemez."
                });
            }
        });
    }
}