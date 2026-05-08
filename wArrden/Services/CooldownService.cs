using wArrden.Data;
using Microsoft.EntityFrameworkCore;

namespace wArrden.Services;

public interface ICooldownService
{
    Task CleanExpiredAsync(string instance, string category, TimeSpan cooldown, CancellationToken ct);
    Task<HashSet<int>> GetCooldownIdsAsync(string instance, string category, CancellationToken ct);
    Task MarkSearchedAsync(string instance, string category, int[] itemIds, CancellationToken ct);
}

public class CooldownService : ICooldownService
{
    private readonly WardenDbContext _db;

    public CooldownService(WardenDbContext db)
    {
        _db = db;
    }

    public async Task CleanExpiredAsync(string instance, string category, TimeSpan cooldown, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - cooldown;
        await _db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category && e.SearchedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<HashSet<int>> GetCooldownIdsAsync(string instance, string category, CancellationToken ct)
    {
        var ids = await _db.CooldownEntries
            .Where(e => e.Instance == instance && e.Category == category)
            .Select(e => e.ItemId)
            .ToListAsync(ct);

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
    }
}
