using System.Text.RegularExpressions;

namespace ArrWarden.Configuration;

internal static partial class DurationParser
{
    [GeneratedRegex(@"(\d+)\s*([dhms])", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();

    public static TimeSpan Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Duration cannot be empty.");

        var span = TimeSpan.Zero;
        var matches = DurationRegex().Matches(input);
        if (matches.Count == 0)
            throw new ArgumentException($"Invalid duration format: '{input}'. Expected e.g. '30d', '12h', '1h30m', '90s'.");

        foreach (Match match in matches)
        {
            var value = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value.ToLowerInvariant();

            span = unit switch
            {
                "d" => span.Add(TimeSpan.FromDays(value)),
                "h" => span.Add(TimeSpan.FromHours(value)),
                "m" => span.Add(TimeSpan.FromMinutes(value)),
                "s" => span.Add(TimeSpan.FromSeconds(value)),
                _ => throw new ArgumentException($"Unknown duration unit: '{unit}'")
            };
        }

        if (span == TimeSpan.Zero)
            throw new ArgumentException($"Duration '{input}' resolves to zero.");

        return span;
    }
}
