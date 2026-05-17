namespace wArrden.Configuration;

public class WardenOptions
{
    public const string Section = "Warden";

    public string? DryRun { get; set; }
    public bool IsDryRun => string.Equals(DryRun, "true", StringComparison.OrdinalIgnoreCase);
    public string? Timezone { get; set; }
    public string DatabasePath { get; set; } = "data/warden.db";
}
