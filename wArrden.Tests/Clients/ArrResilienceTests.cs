using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using wArrden.Clients;
using wArrden.Clients.Http;

namespace wArrden.Tests;

public class ArrResilienceTests
{
    private const string EmptyPage = "{\"page\":1,\"pageSize\":100,\"totalRecords\":0,\"records\":[]}";

    private static HttpClient BuildClient(
        HttpMessageHandler handler, int retryCount = 3, TimeSpan? attemptTimeout = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test", c =>
            {
                c.BaseAddress = new Uri("http://arr.test/");
                c.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            // Tiny base delay keeps retry tests fast while exercising the real pipeline.
            .AddArrResilience(retryCount, attemptTimeout ?? TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(1));

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
    }

    [Fact]
    public async Task TransientStatus_RecoversOnRetry()
    {
        var handler = new SequencedHttpMessageHandler(EmptyPage, HttpStatusCode.BadGateway, HttpStatusCode.OK);
        var client = new SonarrV3Client(BuildClient(handler), "Sonarr");

        var result = await client.GetQueueAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(2, handler.CallCount); // 502 then 200
    }

    [Fact]
    public async Task TransientException_RecoversOnRetry()
    {
        var handler = new SequencedHttpMessageHandler(
            EmptyPage, new HttpRequestException("connection refused"), HttpStatusCode.OK);
        var client = new SonarrV3Client(BuildClient(handler), "Sonarr");

        var result = await client.GetQueueAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task NonTransientStatus_IsNotRetried(HttpStatusCode code)
    {
        var handler = new SequencedHttpMessageHandler(EmptyPage, code);
        var client = new SonarrV3Client(BuildClient(handler), "Sonarr");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetQueueAsync(CancellationToken.None));
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task PersistentTransient_ThrowsAfterExhaustingRetries()
    {
        var handler = new SequencedHttpMessageHandler(EmptyPage, HttpStatusCode.BadGateway);
        var client = new SonarrV3Client(BuildClient(handler, retryCount: 3), "Sonarr");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetQueueAsync(CancellationToken.None));
        Assert.Equal(4, handler.CallCount); // initial attempt + 3 retries
    }

    [Fact]
    public async Task PerAttemptTimeout_TimesOutFirstAttemptThenRetries()
    {
        var handler = new DelayThenOkHandler(TimeSpan.FromMilliseconds(400), EmptyPage);
        var client = new SonarrV3Client(
            BuildClient(handler, attemptTimeout: TimeSpan.FromMilliseconds(60)), "Sonarr");

        var result = await client.GetQueueAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(2, handler.CallCount); // attempt 1 times out, attempt 2 succeeds
    }

    private sealed class DelayThenOkHandler : HttpMessageHandler
    {
        private readonly TimeSpan _firstDelay;
        private readonly string _body;
        private int _calls;

        public int CallCount => _calls;

        public DelayThenOkHandler(TimeSpan firstDelay, string body)
        {
            _firstDelay = firstDelay;
            _body = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var n = Interlocked.Increment(ref _calls);
            if (n == 1)
                await Task.Delay(_firstDelay, ct); // exceeds the per-attempt timeout on the first attempt

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
