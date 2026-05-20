using wArrden.Data;
using wArrden.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace wArrden.Tests;

public class CooldownServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<WardenDbContext> _options;

    public CooldownServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<WardenDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new WardenDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private WardenDbContext CreateContext() => new(_options);

    [Fact]
    public async Task CleanExpiredAsync_RemovesEntriesOlderThanCooldown()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-40)
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 2,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await service.CleanExpiredAsync("Sonarr", "Missing", TimeSpan.FromDays(30), CancellationToken.None);

        var remaining = await db.CooldownEntries.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(2, remaining[0].ItemId);
    }

    [Fact]
    public async Task CleanExpiredAsync_RespectsInstanceAndCategory()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-40)
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Radarr", Category = "Missing", ItemId = 2,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-40)
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await service.CleanExpiredAsync("Sonarr", "Missing", TimeSpan.FromDays(30), CancellationToken.None);

        var remaining = await db.CooldownEntries.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(2, remaining[0].ItemId);
    }

    [Fact]
    public async Task GetCooldownIdsAsync_ReturnsCorrectIds()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 10,
            SearchedAtUtc = DateTime.UtcNow
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 20,
            SearchedAtUtc = DateTime.UtcNow
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Upgrade", ItemId = 30,
            SearchedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        var ids = await service.GetCooldownIdsAsync("Sonarr", "Missing", CancellationToken.None);

        Assert.Equal(2, ids.Count);
        Assert.Contains(10, ids);
        Assert.Contains(20, ids);
        Assert.DoesNotContain(30, ids);
    }

    [Fact]
    public async Task MarkSearchedAsync_AddsEntries()
    {
        using var db = CreateContext();
        var service = new CooldownService(db);
        await service.MarkSearchedAsync("Radarr", "Upgrade", new[] { 1, 2, 3 }, CancellationToken.None);

        var entries = await db.CooldownEntries.ToListAsync();
        Assert.Equal(3, entries.Count);
        Assert.All(entries, e => Assert.Equal("Radarr", e.Instance));
        Assert.All(entries, e => Assert.Equal("Upgrade", e.Category));
    }

    [Fact]
    public async Task CleanExpiredAsync_SameTypeInstancesIsolated()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Series", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-40)
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Anime", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-40)
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await service.CleanExpiredAsync("Series", "Missing", TimeSpan.FromDays(30), CancellationToken.None);

        var remaining = await db.CooldownEntries.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("Anime", remaining[0].Instance);
        Assert.Equal(1, remaining[0].ItemId);
    }

    [Fact]
    public async Task GetCooldownIdsAsync_SameTypeInstancesIsolated()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Series", Category = "Missing", ItemId = 10,
            SearchedAtUtc = DateTime.UtcNow
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Anime", Category = "Missing", ItemId = 20,
            SearchedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        var ids = await service.GetCooldownIdsAsync("Series", "Missing", CancellationToken.None);

        Assert.Single(ids);
        Assert.Contains(10, ids);
        Assert.DoesNotContain(20, ids);
    }

    [Fact]
    public async Task GetCooldownIdsAsync_NoEntries_ReturnsEmpty()
    {
        using var db = CreateContext();
        var service = new CooldownService(db);
        var ids = await service.GetCooldownIdsAsync("Nonexistent", "Missing", CancellationToken.None);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task MarkSearchedAsync_EmptyArray_NoOp()
    {
        using var db = CreateContext();
        var service = new CooldownService(db);
        await service.MarkSearchedAsync("Sonarr", "Missing", Array.Empty<int>(), CancellationToken.None);

        var entries = await db.CooldownEntries.ToListAsync();
        Assert.Empty(entries);
    }

    [Fact]
    public async Task MarkSearchedAsync_DuplicateItemId_ThrowsDbUpdateException()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.MarkSearchedAsync("Sonarr", "Missing", new[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task CleanExpiredAsync_NoneExpired_RemovesNothing()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-5)
        });
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 2,
            SearchedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await service.CleanExpiredAsync("Sonarr", "Missing", TimeSpan.FromDays(30), CancellationToken.None);

        var remaining = await db.CooldownEntries.ToListAsync();
        Assert.Equal(2, remaining.Count);
    }

    [Fact]
    public async Task CleanExpiredAsync_ExactlyAtCooldown_RemovesEntry()
    {
        using var db = CreateContext();
        db.CooldownEntries.Add(new CooldownEntry
        {
            Instance = "Sonarr", Category = "Missing", ItemId = 1,
            SearchedAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        await db.SaveChangesAsync();

        var service = new CooldownService(db);
        await service.CleanExpiredAsync("Sonarr", "Missing", TimeSpan.FromDays(30), CancellationToken.None);

        var remaining = await db.CooldownEntries.ToListAsync();
        Assert.Empty(remaining);
    }
}
