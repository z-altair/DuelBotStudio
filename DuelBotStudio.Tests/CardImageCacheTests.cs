using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Media.Imaging;
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

    [TestMethod]
    public async Task GetCardImageAsync_CoalescesConcurrentDownloadsForTheSameCard()
    {
        const int cardId = 654;
        string cachePath = Path.Combine(Path.GetTempPath(), $"DuelBotStudio.Images-{Guid.NewGuid():N}");
        byte[] validImage = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");
        int requestCount = 0;
        using var httpClient = new HttpClient(new AsyncStubHttpMessageHandler(async () =>
        {
            Interlocked.Increment(ref requestCount);
            await Task.Delay(40);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(validImage)
            };
        }));

        try
        {
            var cache = new CardImageCache(httpClient, cachePath);

            BitmapImage[] images = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => cache.GetCardImageAsync(cardId)));

            Assert.AreEqual(1, requestCount);
            Assert.IsTrue(images.All(image => ReferenceEquals(images[0], image)));
        }
        finally
        {
            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, true);
            }
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory());
    }

    private sealed class AsyncStubHttpMessageHandler(Func<Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory();
    }
}
