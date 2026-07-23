using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using wArrden.Services;

namespace wArrden.Tests;

public class ArrFailureTests
{
    public static IEnumerable<object[]> EnvironmentalExceptions() => new List<object[]>
    {
        new object[] { new HttpRequestException("boom") },
        new object[] { new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway) },
        new object[] { new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized) },
        new object[] { new SocketException() },
        new object[] { new TimeoutRejectedException() },
        new object[] { new BrokenCircuitException() },
        new object[] { new TaskCanceledException() },
        new object[] { new OperationCanceledException() },
    };

    [Theory]
    [MemberData(nameof(EnvironmentalExceptions))]
    public void IsEnvironmental_TrueForArrCommunicationFailures(Exception ex)
    {
        Assert.True(ArrFailure.IsEnvironmental(ex));
    }

    public static IEnumerable<object[]> DefectExceptions() => new List<object[]>
    {
        new object[] { new JsonException("bad json") },
        new object[] { new InvalidOperationException("no handler") },
        new object[] { new NullReferenceException() },
        new object[] { new ArgumentException("bad arg") },
    };

    [Theory]
    [MemberData(nameof(DefectExceptions))]
    public void IsEnvironmental_FalseForWArrdenDefects(Exception ex)
    {
        Assert.False(ArrFailure.IsEnvironmental(ex));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void IsAuthFailure_TrueFor401And403(HttpStatusCode code)
    {
        var ex = new HttpRequestException("auth", null, code);
        Assert.True(ArrFailure.IsAuthFailure(ex));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void IsAuthFailure_FalseForOtherStatusCodes(HttpStatusCode code)
    {
        var ex = new HttpRequestException("other", null, code);
        Assert.False(ArrFailure.IsAuthFailure(ex));
    }

    [Fact]
    public void IsAuthFailure_FalseForNonHttpException()
    {
        Assert.False(ArrFailure.IsAuthFailure(new TimeoutRejectedException()));
    }
}
