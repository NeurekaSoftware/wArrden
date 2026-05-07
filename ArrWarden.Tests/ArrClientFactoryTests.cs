using ArrWarden.Clients;

namespace ArrWarden.Tests;

public class ArrClientFactoryTests
{
    [Fact]
    public void CreateSonarr_WithVersion3_ReturnsSonarrV3Client()
    {
        var client = ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "3");
        Assert.IsType<SonarrV3Client>(client);
        Assert.Equal("Sonarr", client.Instance);
    }

    [Fact]
    public void CreateSonarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "4"));
    }

    [Fact]
    public void CreateSonarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "2"));
    }

    [Fact]
    public void CreateRadarr_WithVersion3_ReturnsRadarrV3Client()
    {
        var client = ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "3");
        Assert.IsType<RadarrV3Client>(client);
        Assert.Equal("Radarr", client.Instance);
    }

    [Fact]
    public void CreateRadarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "4"));
    }
}
