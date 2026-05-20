using System.Net;
using System.Text;

namespace wArrden.Tests;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    public string? LastRequestUri { get; private set; }

    private readonly string _responseJson;

    public FakeHttpMessageHandler(string responseJson)
    {
        _responseJson = responseJson;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri?.ToString();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
