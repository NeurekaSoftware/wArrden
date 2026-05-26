namespace wArrden.Clients;

public static class ArrClientFactory
{
    public static IArrClient CreateSonarr(string url, string apiKey, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v3", StringComparison.Ordinal))
            throw new NotSupportedException($"Sonarr API version {apiVersion} is not supported. Only version v3 is supported.");

        return new SonarrV3Client(url, apiKey, instanceName);
    }

    public static IArrClient CreateRadarr(string url, string apiKey, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v3", StringComparison.Ordinal))
            throw new NotSupportedException($"Radarr API version {apiVersion} is not supported. Only version v3 is supported.");

        return new RadarrV3Client(url, apiKey, instanceName);
    }

    public static IArrClient CreateLidarr(string url, string apiKey, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v1", StringComparison.Ordinal))
            throw new NotSupportedException($"Lidarr API version {apiVersion} is not supported. Only version v1 is supported.");

        return new LidarrV1Client(url, apiKey, instanceName);
    }

    public static IArrClient CreateWhisparr(string url, string apiKey, string apiVersion, string instanceName)
    {
        return NormalizeApiVersion(apiVersion) switch
        {
            "v3" => new WhisparrV3Client(url, apiKey, instanceName),
            "v3-eros" => new WhisparrV3ErosClient(url, apiKey, instanceName),
            _ => throw new NotSupportedException(
                $"Whisparr API version {apiVersion} is not supported. Only versions v3 and v3-eros are supported.")
        };
    }

    private static string NormalizeApiVersion(string apiVersion) => apiVersion.Trim().ToLowerInvariant();
}
