using wArrden.Data;
using Microsoft.EntityFrameworkCore;

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
    private readonly WardenDbContext _db;
    private readonly OutputService _output;

    public CooldownService(WardenDbContext db, OutputService output)
    {
        _db = db;
        _output = output;
    }

    public async Task CleanExpiredAsync(string instance, string category, TimeSpan cooldown, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - cooldown;
        var deleted = await _db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category && e.SearchedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"Cleaned {deleted} expired cooldown entries for {category}");
    }

    public async Task<HashSet<int>> GetCooldownIdsAsync(string instance, string category, CancellationToken ct)
    {
        var ids = await _db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

        _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"{ids.Count} items on cooldown for {category}");

        return new HashSet<int>(ids);
    }

    public async Task MarkSearchedAsync(string instance, string category, int[] itemIds, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var entries = itemIds.Select(id => new CooldownEntry
        {
            Instance = instance,
            Category = category,
            ItemId = id,
            SearchedAtUtc = now
        });

        await _db.CooldownEntries.AddRangeAsync(entries, ct);
        await _db.SaveChangesAsync(ct);

        _output.WriteDebug($"{instance.ToLowerInvariant()}.cooldown", $"Marked {itemIds.Length} items as searched for {category}");
    }

    public async Task<int> ClearAllAsync(string category, string? instance, CancellationToken ct)
    {
        var categories = new[] { category, $"{category}_Season" };
        var query = _db.CooldownEntries.Where(e => categories.Contains(e.Category));
        if (instance is not null)
            query = query.Where(e => e.Instance == instance);

        var count = await query.ExecuteDeleteAsync(ct);

        _output.WriteDebug($"{(instance ?? "all").ToLowerInvariant()}.cooldown", $"Cleared {count} cooldown entries for {category}");

        return count;
    }
}
