using wArrden.Services;

namespace wArrden.Tests;

public class NullSearchOutputWriter : OutputService.SearchOutputWriter
{
    public NullSearchOutputWriter() : base("test", "Missing Search", 10, TextWriter.Null, true) { }

    public override void WriteHeader() { }
    public override void SetPhase(string phase) { }
    public override void WriteStats(int totalCount, int onCooldown, int eligible, int searched, bool isLast, string? resultOverride = null) { }
    public override void StartResults() { }
    public override void WriteItem(string title) { }
    public override void WriteTrailer() { }
}
