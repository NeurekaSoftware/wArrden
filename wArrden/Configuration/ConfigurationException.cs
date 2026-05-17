namespace wArrden.Configuration;

public class ConfigurationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ConfigurationException(IReadOnlyList<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }
}
