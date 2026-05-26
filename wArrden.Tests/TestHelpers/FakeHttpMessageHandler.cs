using System.Net;
using System.Text;

namespace wArrden.Tests;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    public string? LastRequestUri { get; private set; }
    public HttpMethod? LastRequestMethod { get; private set; }
    public string? LastRequestBody { get; private set; }

    private readonly string _responseJson;
    private readonly HttpStatusCode _statusCode;
    private readonly Exception? _exception;

    public FakeHttpMessageHandler(string responseJson)
        : this(responseJson, HttpStatusCode.OK, null) { }

    public FakeHttpMessageHandler(HttpStatusCode statusCode)
        : this("", statusCode, null) { }

    public FakeHttpMessageHandler(Exception exception)
        : this("", HttpStatusCode.OK, exception) { }

    private FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode, Exception? exception)
    {
        _responseJson = responseJson;
        _statusCode = statusCode;
        _exception = exception;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri?.ToString();
        LastRequestMethod = request.Method;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        if (_exception is not null)
            throw _exception;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
        };
        return response;
    }
}
