using wArrden.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace wArrden.Services;

public interface ICooldownService
{
    Task CleanExpiredAsync(string instance, string category, TimeSpan cooldown, CancellationToken ct);
    Task<HashSet<int>> GetCooldownIdsAsync(string instance, string category, CancellationToken ct);
    Task MarkSearchedAsync(string instance, string category, int[] itemIds, CancellationToken ct);
    Task<int> ClearAllAsync(string category, string? instance, CancellationToken ct);
}

public class CooldownService : ICooldownService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutputService _output;

    public CooldownService(IServiceScopeFactory scopeFactory, OutputService output)
    {
        _scopeFactory = scopeFactory;
        _output = output;
    }

    public async Task CleanExpiredAsync(string instance, string category, TimeSpan cooldown, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var cutoff = DateTime.UtcNow - cooldown;
        var deleted = await db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category && e.SearchedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"Cleaned {deleted} expired cooldown entries for {category}");
    }

    public async Task<HashSet<int>> GetCooldownIdsAsync(string instance, string category, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var ids = await db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"{ids.Count} items on cooldown for {category}");

        return new HashSet<int>(ids);
    }

    public async Task MarkSearchedAsync(string instance, string category, int[] itemIds, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var now = DateTime.UtcNow;
        var entries = itemIds.Select(id => new CooldownEntry
        {
            Instance = instance,
            Category = category,
            ItemId = id,
            SearchedAtUtc = now
        });

        await db.CooldownEntries.AddRangeAsync(entries, ct);
        await db.SaveChangesAsync(ct);

        _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"Marked {itemIds.Length} items as searched for {category}");
    }

    public async Task<int> ClearAllAsync(string category, string? instance, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WardenDbContext>();

        var categories = new[] { category, $"{category}_Season" };
        var query = db.CooldownEntries.Where(e => categories.Contains(e.Category));
        if (instance is not null)
            query = query.Where(e => e.Instance == instance);

        var count = await query.ExecuteDeleteAsync(ct);

        _output.WriteDebug($"{(instance ?? "all").ToLowerInvariant()}.cooldown", $"Cleared {count} cooldown entries for {category}");

        return count;
    }
}
