using System.Net;
using System.Text;

namespace wArrden.Tests;

/// <summary>
/// Test handler that replays a scripted sequence of outcomes — a status code (returns a
/// response) or an exception (throws) — one per call, exposing a call count so tests can
/// assert how many attempts the resilience pipeline made. Once the script is exhausted, the
/// final step repeats.
/// </summary>
internal sealed class SequencedHttpMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyList<object> _steps; // HttpStatusCode or Exception
    private readonly string _body;
    private int _index = -1;

    public int CallCount { get; private set; }

    public SequencedHttpMessageHandler(string body, params object[] steps)
    {
        _body = body;
        _steps = steps;
    }

    public SequencedHttpMessageHandler(params object[] steps) : this("{}", steps) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        _index = Math.Min(_index + 1, _steps.Count - 1);
        var step = _steps[_index];

        if (step is Exception ex)
            return Task.FromException<HttpResponseMessage>(ex);

        var status = (HttpStatusCode)step;
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
