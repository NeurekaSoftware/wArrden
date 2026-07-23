namespace wArrden.Clients;

public static class ArrClientFactory
{
    public static IArrClient CreateSonarr(HttpClient http, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v3", StringComparison.Ordinal))
            throw new NotSupportedException($"Sonarr API version {apiVersion} is not supported. Only version v3 is supported.");

        return new SonarrV3Client(http, instanceName);
    }

    public static IArrClient CreateRadarr(HttpClient http, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v3", StringComparison.Ordinal))
            throw new NotSupportedException($"Radarr API version {apiVersion} is not supported. Only version v3 is supported.");

        return new RadarrV3Client(http, instanceName);
    }

    public static IArrClient CreateLidarr(HttpClient http, string apiVersion, string instanceName)
    {
        if (!string.Equals(NormalizeApiVersion(apiVersion), "v1", StringComparison.Ordinal))
            throw new NotSupportedException($"Lidarr API version {apiVersion} is not supported. Only version v1 is supported.");

        return new LidarrV1Client(http, instanceName);
    }

    public static IArrClient CreateWhisparr(HttpClient http, string apiVersion, string instanceName)
    {
        return NormalizeApiVersion(apiVersion) switch
        {
            "v3" => new WhisparrV3Client(http, instanceName),
            "v3-eros" => new WhisparrV3ErosClient(http, instanceName),
            _ => throw new NotSupportedException(
                $"Whisparr API version {apiVersion} is not supported. Only versions v3 and v3-eros are supported.")
        };
    }

    private static string NormalizeApiVersion(string apiVersion) => apiVersion.Trim().ToLowerInvariant();
}
