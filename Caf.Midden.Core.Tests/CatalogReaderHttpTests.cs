using Caf.Midden.Core.Services;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class CatalogReaderHttpTests
{
    [Fact]
    public async Task Read_AbsoluteUrl_RequestsThatOrigin()
    {
        Uri? requestedUri = null;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestedUri = request.RequestUri)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                    {
                      "schemaVersion": "v0.2",
                      "creationDate": "2026-09-03T00:00:00Z",
                      "metadatas": [],
                      "projects": []
                    }
                    """),
            });

        using var client = new HttpClient(handler.Object, disposeHandler: false)
        {
            BaseAddress = new Uri("https://app.example.org/midden/"),
        };
        var reader = new CatalogReaderHttp(client);

        await reader.Read("https://catalogs.example.net/current/catalog.json", true);

        Assert.NotNull(requestedUri);
        Assert.Equal("catalogs.example.net", requestedUri.Host);
        Assert.Equal("/current/catalog.json", requestedUri.AbsolutePath);
        Assert.False(string.IsNullOrWhiteSpace(requestedUri.Query));
        handler.VerifyAll();
    }
}