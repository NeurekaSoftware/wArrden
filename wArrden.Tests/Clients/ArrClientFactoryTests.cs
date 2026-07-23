using wArrden.Clients;

namespace wArrden.Tests;

public class ArrClientFactoryTests
{
    private static HttpClient Http() => new() { BaseAddress = new Uri("http://localhost/") };

    [Fact]
    public void CreateSonarr_WithApiVersionV3_ReturnsSonarrV3Client()
    {
        var client = ArrClientFactory.CreateSonarr(Http(), "v3", "MySonarr");
        Assert.IsType<SonarrV3Client>(client);
        Assert.Equal("MySonarr", client.Instance);
    }

    [Fact]
    public void CreateSonarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr(Http(), "4", "Test"));
    }

    [Fact]
    public void CreateSonarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateSonarr(Http(), "2", "Test"));
    }

    [Fact]
    public void CreateRadarr_WithApiVersionV3_ReturnsRadarrV3Client()
    {
        var client = ArrClientFactory.CreateRadarr(Http(), "v3", "MyRadarr");
        Assert.IsType<RadarrV3Client>(client);
        Assert.Equal("MyRadarr", client.Instance);
    }

    [Fact]
    public void CreateRadarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr(Http(), "4", "Test"));
    }

    [Fact]
    public void CreateRadarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr(Http(), "2", "Test"));
    }

    [Fact]
    public void CreateLidarr_WithApiVersionV1_ReturnsLidarrV1Client()
    {
        var client = ArrClientFactory.CreateLidarr(Http(), "v1", "MyLidarr");
        Assert.IsType<LidarrV1Client>(client);
        Assert.Equal("MyLidarr", client.Instance);
    }

    [Fact]
    public void CreateLidarr_WithVersion3_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateLidarr(Http(), "3", "Test"));
    }

    [Fact]
    public void CreateWhisparr_WithApiVersionV3_ReturnsWhisparrV3Client()
    {
        var client = ArrClientFactory.CreateWhisparr(Http(), "v3", "MyWhisparr");
        Assert.IsType<WhisparrV3Client>(client);
        Assert.Equal("MyWhisparr", client.Instance);
    }

    [Fact]
    public void CreateWhisparr_WithApiVersionV3Eros_ReturnsWhisparrV3ErosClient()
    {
        var client = ArrClientFactory.CreateWhisparr(Http(), "v3-eros", "MyWhisparr");
        Assert.IsType<WhisparrV3ErosClient>(client);
        Assert.Equal("MyWhisparr", client.Instance);
    }

    [Fact]
    public void CreateWhisparr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateWhisparr(Http(), "4", "Test"));
    }

    [Fact]
    public void CreateLidarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateLidarr(Http(), "2", "Test"));
    }

    [Fact]
    public void CreateWhisparr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateWhisparr(Http(), "2", "Test"));
    }
}
