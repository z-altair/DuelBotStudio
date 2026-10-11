using System.IO;
using System.Net;
using System.Net.Http;
using DuelBotStudio.Services;

namespace DuelBotStudio.Tests;

[TestClass]
public sealed class CardImageCacheTests
{
    [TestMethod]
    public async Task GetCardImageAsync_ReplacesCorruptDiskImageAndCachesTheResult()
    {
        const int cardId = 321;
        string cachePath = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.Images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        string imagePath = Path.Combine(cachePath, $"{cardId}.jpg");
        await File.WriteAllTextAsync(imagePath, "corrupt image");
        byte[] validImage = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");
        int requestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(() =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(validImage)
            };
        }));

        try
        {
            var cache = new CardImageCache(httpClient, cachePath);

            var image = await cache.GetCardImageAsync(cardId);
            var cachedAgain = await cache.GetCardImageAsync(cardId);

            Assert.AreEqual(1, image.PixelWidth);
            Assert.AreEqual(1, image.PixelHeight);
            Assert.AreSame(image, cachedAgain);
            Assert.AreEqual(1, requestCount);
            CollectionAssert.AreEqual(validImage, await File.ReadAllBytesAsync(imagePath));
        }
        finally
        {
            Directory.Delete(cachePath, true);
        }
    }
    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory());
    }
}
