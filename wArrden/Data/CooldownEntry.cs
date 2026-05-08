using Microsoft.EntityFrameworkCore;

namespace wArrden.Data;

public class CooldownEntry
{
    public long Id { get; set; }
    public string Instance { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public DateTime SearchedAtUtc { get; set; }
}
