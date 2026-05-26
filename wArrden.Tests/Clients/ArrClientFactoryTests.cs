using wArrden.Clients;

namespace wArrden.Tests;

public class ArrClientFactoryTests
{
    [Fact]
    public void CreateSonarr_WithApiVersionV3_ReturnsSonarrV3Client()
    {
        var client = ArrClientFactory.CreateSonarr("http://localhost:8989", "api-key", "v3", "MySonarr");
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
    public void CreateRadarr_WithApiVersionV3_ReturnsRadarrV3Client()
    {
        var client = ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "v3", "MyRadarr");
        Assert.IsType<RadarrV3Client>(client);
        Assert.Equal("MyRadarr", client.Instance);
    }

    [Fact]
    public void CreateRadarr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "4", "Test"));
    }

    [Fact]
    public void CreateRadarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateRadarr("http://localhost:7878", "api-key", "2", "Test"));
    }

    [Fact]
    public void CreateLidarr_WithApiVersionV1_ReturnsLidarrV1Client()
    {
        var client = ArrClientFactory.CreateLidarr("http://localhost:8686", "api-key", "v1", "MyLidarr");
        Assert.IsType<LidarrV1Client>(client);
        Assert.Equal("MyLidarr", client.Instance);
    }

    [Fact]
    public void CreateLidarr_WithVersion3_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateLidarr("http://localhost:8686", "api-key", "3", "Test"));
    }

    [Fact]
    public void CreateWhisparr_WithApiVersionV3_ReturnsWhisparrV3Client()
    {
        var client = ArrClientFactory.CreateWhisparr("http://localhost:6969", "api-key", "v3", "MyWhisparr");
        Assert.IsType<WhisparrV3Client>(client);
        Assert.Equal("MyWhisparr", client.Instance);
    }

    [Fact]
    public void CreateWhisparr_WithApiVersionV3Eros_ReturnsWhisparrV3ErosClient()
    {
        var client = ArrClientFactory.CreateWhisparr("http://localhost:6969", "api-key", "v3-eros", "MyWhisparr");
        Assert.IsType<WhisparrV3ErosClient>(client);
        Assert.Equal("MyWhisparr", client.Instance);
    }

    [Fact]
    public void CreateWhisparr_WithVersion4_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateWhisparr("http://localhost:6969", "api-key", "4", "Test"));
    }

    [Fact]
    public void CreateLidarr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateLidarr("http://localhost:8686", "api-key", "2", "Test"));
    }

    [Fact]
    public void CreateWhisparr_WithVersion2_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            ArrClientFactory.CreateWhisparr("http://localhost:6969", "api-key", "2", "Test"));
    }
}
