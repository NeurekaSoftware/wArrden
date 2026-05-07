using Microsoft.EntityFrameworkCore;

namespace ArrWarden.Data;

public class WardenDbContext : DbContext
{
    public DbSet<CooldownEntry> CooldownEntries => Set<CooldownEntry>();

    public WardenDbContext(DbContextOptions<WardenDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CooldownEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Instance, x.Category, x.ItemId }).IsUnique();
            e.Property(x => x.Instance).IsRequired().HasMaxLength(16);
            e.Property(x => x.Category).IsRequired().HasMaxLength(16);
        });
    }
}
