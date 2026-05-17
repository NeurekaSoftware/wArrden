using wArrden.Clients;

namespace wArrden.Tests;

public class ArrClientFactoryTests
{
    [Fact]
    public void CreateSonarr_WithVersion3_ReturnsSonarrV3Client()
    {
        var client = ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "3", "MySonarr");
        Assert.IsType<SonarrV3Client>(client);
        Assert.Equal("MySonarr", client.Instance);
    }

    [Fact]
    public void CreateSonarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "4", "Test"));
    }

    [Fact]
    public void CreateSonarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "2", "Test"));
    }

    [Fact]
    public void CreateRadarr_WithVersion3_ReturnsRadarrV3Client()
    {
        var client = ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "3", "MyRadarr");
        Assert.IsType<RadarrV3Client>(client);
        Assert.Equal("MyRadarr", client.Instance);
    }

    [Fact]
    public void CreateRadarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "4", "Test"));
    }
}
