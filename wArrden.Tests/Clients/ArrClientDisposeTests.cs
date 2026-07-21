using wArrden.Clients;

namespace wArrden.Tests;

public class ArrClientDisposeTests
{
    // Each client owns a single HttpClient torn down in Dispose(). Dispose() is called
    // from the host's ApplicationStopped callback and must stay safe to call more than
    // once (double-dispose is a no-op, never ObjectDisposedException).
    [Fact]
    public void Dispose_IsIdempotent_ForAllClients()
    {
        var factories = new Func<HttpMessageHandler, IArrClient>[]
        {
            h => new SonarrV3Client("http://localhost", "key", "Sonarr", h),
            h => new RadarrV3Client("http://localhost", "key", "Radarr", h),
            h => new LidarrV1Client("http://localhost", "key", "Lidarr", h),
            h => new WhisparrV3Client("http://localhost", "key", "Whisparr", h),
            h => new WhisparrV3ErosClient("http://localhost", "key", "Whisparr Eros", h),
        };

        foreach (var factory in factories)
        {
            var client = factory(new FakeHttpMessageHandler("{}"));

            client.Dispose();

            var secondDispose = Record.Exception(() => client.Dispose());
            Assert.Null(secondDispose);
        }
    }
}
