namespace wArrden.Clients;

public static class ArrClientFactory
{
    public static IArrClient CreateSonarr(string url, string apiKey, string apiVersion, string instanceName)
    {
        if (apiVersion != "3")
            throw new NotSupportedException($"Sonarr API version {apiVersion} is not supported. Only version 3 is supported.");

        return new SonarrV3Client(url, apiKey, instanceName);
    }

    public static IArrClient CreateRadarr(string url, string apiKey, string apiVersion, string instanceName)
    {
        if (apiVersion != "3")
            throw new NotSupportedException($"Radarr API version {apiVersion} is not supported. Only version 3 is supported.");

        return new RadarrV3Client(url, apiKey, instanceName);
    }
}
