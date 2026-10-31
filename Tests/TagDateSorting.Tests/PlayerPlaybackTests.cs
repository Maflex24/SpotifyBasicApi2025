using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Frontend;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using SpotifyBasicApi2025.Controllers;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace TagDateSorting.Tests;

public class PlayerPlaybackTests
{
    private const string Track = "spotify:track:selected";
    private const string Playlist = "spotify:playlist:selected";
    private const string Album = "spotify:album:selected";

    [Test]
    public async Task PlayTargetsActiveDeviceAndPreservesJsonBody()
    {
        var handler = new SpotifyHandler();
        var payload = JsonSerializer.SerializeToElement(new { uris = new[] { Track }, position_ms = 0 });

        Assert.That((await CreatePlayer(handler).PlayAsync(body: payload)).Started, Is.True);

        var play = handler.Requests.Single(r => r.Method == HttpMethod.Put);
        Assert.That(play.Uri.PathAndQuery, Is.EqualTo("/v1/me/player/play?device_id=active%20device"));
        Assert.That(play.Body, Is.EqualTo(payload.GetRawText()));
        Assert.That(handler.PlayCount, Is.EqualTo(1));
        Assert.That(handler.TransferCount, Is.Zero);
    }

    [Test]
    public async Task AcceptedButIgnoredContextIsRetriedOnSameDevice()
    {
        var handler = new SpotifyHandler { IgnoreFirstPlay = true, ContextPlayback = true };
        var payload = JsonSerializer.SerializeToElement(new { context_uri = Playlist, offset = new { position = 0 }, position_ms = 0 });

        Assert.That((await CreatePlayer(handler).PlayAsync(body: payload)).Started, Is.True);

        var plays = handler.Requests.Where(r => r.Uri.AbsolutePath.EndsWith("/play")).ToList();
        Assert.That(plays, Has.Count.EqualTo(2));
        Assert.That(plays.Select(r => r.Body), Is.All.EqualTo(payload.GetRawText()));
        Assert.That(plays.Select(r => r.Uri.Query), Is.All.EqualTo("?device_id=active%20device"));
        Assert.That(handler.TransferCount, Is.EqualTo(1));
        var transfer = handler.Requests.Single(r => r.Method == HttpMethod.Put && r.Uri.AbsolutePath == "/v1/me/player");
        var body = JsonDocument.Parse(transfer.Body!).RootElement;
        Assert.That(body.GetProperty("play").GetBoolean(), Is.False);
        Assert.That(body.GetProperty("device_ids")[0].GetString(), Is.EqualTo("active device"));
    }

    [TestCase(false, 1)]
    [TestCase(false, 4)]
    [TestCase(true, 4)]
    public async Task BrokenUrisUseAlbumAndQueueRemainingTracksInOrder(bool relinked, int count)
    {
        var handler = new SpotifyHandler { IgnoreUrisPlayback = true, Relinked = relinked };
        // The old track is also in the requested list: it must not count as a successful start.
        var uris = new[] { Track, "spotify:track:old", "spotify:track:third", "spotify:track:fourth" }.Take(count).ToArray();
        var result = await CreatePlayer(handler).PlayAsync(body: new { uris, position_ms = 42000 });

        Assert.That(result.Started, Is.True);
        Assert.That(result.Warning, Does.Contain("obejścia"));
        Assert.That(handler.TransferCount, Is.Zero, "Do not repeat a transfer known not to fix the broken uris command.");
        var plays = handler.Requests.Where(r => r.Uri.AbsolutePath.EndsWith("/play")).ToList();
        Assert.That(plays, Has.Count.EqualTo(2));
        var albumBody = JsonDocument.Parse(plays[1].Body!).RootElement;
        Assert.That(albumBody.GetProperty("context_uri").GetString(), Is.EqualTo(Album));
        Assert.That(albumBody.GetProperty("offset").GetProperty("uri").GetString(), Is.EqualTo(relinked ? "spotify:track:replacement" : Track));
        Assert.That(albumBody.GetProperty("position_ms").GetInt32(), Is.EqualTo(42000));
        Assert.That(albumBody.TryGetProperty("uris", out _), Is.False);
        var queue = handler.Requests.Where(r => r.Method == HttpMethod.Post).ToList();
        Assert.That(queue.Select(r => r.Uri.Query), Is.EqualTo(uris.Skip(1).Select(uri => $"?device_id=active%20device&uri={Uri.EscapeDataString(uri)}")));
        Assert.That(handler.Requests.IndexOf(plays[1]), Is.LessThan(queue.Count > 0 ? handler.Requests.IndexOf(queue[0]) : int.MaxValue));
    }

    [Test]
    public async Task PartialQueueFailureIsReportedWithoutRestartingTheTrack()
    {
        var handler = new SpotifyHandler { IgnoreUrisPlayback = true, FailQueueAt = 2 };
        var result = await CreatePlayer(handler).PlayAsync(body: new { uris = new[] { Track, "spotify:track:a", "spotify:track:b", "spotify:track:c" } });
        Assert.That(result.Started, Is.True);
        Assert.That(result.Warning, Does.Contain("tylko 1 z 3"));
        Assert.That(handler.PlayCount, Is.EqualTo(2));
        Assert.That(handler.QueueCount, Is.EqualTo(2));
    }

    [Test]
    public void MissingAlbumDoesNotQueueTracksOrReportSuccess()
    {
        var handler = new SpotifyHandler { IgnoreUrisPlayback = true, MissingAlbum = true };
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => CreatePlayer(handler).PlayAsync(body: new { uris = new[] { Track } }));
        Assert.That(error!.Message, Does.Contain("nie zwróciło albumu"));
        Assert.That(handler.QueueCount, Is.Zero);
        Assert.That(handler.PlayCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ControllerAndFrontendExposeFallbackWarningAsSuccessfulPlayback()
    {
        var handler = new SpotifyHandler { IgnoreUrisPlayback = true };
        var result = await new PlayerController(CreatePlayer(handler), null!).Play(body: new { uris = new[] { Track } });
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var json = JsonSerializer.Serialize(((OkObjectResult)result).Value);
        var playback = new PlaybackService(new HttpClient(new ResponseHandler(HttpStatusCode.OK, json)) { BaseAddress = new Uri("https://app.test/") });
        Assert.That(await playback.PlayAsync(new { uris = new[] { Track } }), Is.True);
        Assert.That(playback.Error, Is.Null);
        Assert.That(playback.Warning, Does.Contain("obejścia"));
        playback.ClearError();
        Assert.That(playback.Warning, Is.Null);
    }

    [Test]
    public async Task InactiveDeviceIsActivatedBeforePlay()
    {
        var handler = new SpotifyHandler { InitiallyActive = false };

        Assert.That((await CreatePlayer(handler).PlayAsync(body: new { uris = new[] { Track } })).Started, Is.True);

        var mutations = handler.Requests.Where(r => r.Method == HttpMethod.Put).ToList();
        Assert.That(mutations.Select(r => r.Uri.AbsolutePath), Is.EqualTo(new[] { "/v1/me/player", "/v1/me/player/play" }));
        Assert.That(handler.ActivationPolls, Is.GreaterThanOrEqualTo(2), "Wait for transfer to become visible before sending play.");
    }

    [Test]
    public async Task UnconfirmedPlaybackReturnsErrorInsteadOf204()
    {
        var handler = new SpotifyHandler { IgnoreAllPlays = true };
        var controller = new PlayerController(CreatePlayer(handler), null!);

        var result = await controller.Play(body: new { uris = new[] { Track } });

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(502));
        Assert.That(((ObjectResult)result).Value, Does.Contain("nie rozpoczęło"));
        Assert.That(handler.PlayCount, Is.EqualTo(2));
    }

    [TestCase("{\"devices\":[]}")]
    [TestCase("{\"devices\":[{\"id\":\"restricted\",\"is_active\":true,\"is_restricted\":true}]}")]
    public void UnavailableDevicesDoNotReceivePlaybackCommands(string devices)
    {
        var handler = new SpotifyHandler { DevicesJson = devices };
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => CreatePlayer(handler).PlayAsync());
        Assert.That(error!.Message, Does.Contain("Brak dostępnego urządzenia"));
        Assert.That(handler.PlayCount, Is.Zero);
        Assert.That(handler.TransferCount, Is.Zero);
    }

    [Test]
    public void ExplicitDeviceIsNotReplacedWithAnotherDevice()
    {
        var handler = new SpotifyHandler();
        Assert.ThrowsAsync<InvalidOperationException>(() => CreatePlayer(handler).PlayAsync("missing"));
        Assert.That(handler.PlayCount, Is.Zero);
    }

    [Test]
    public async Task RelinkedTrackAndResumeAreConfirmed()
    {
        var handler = new SpotifyHandler { Relinked = true };
        var player = CreatePlayer(handler);
        Assert.That((await player.PlayAsync(body: new { uris = new[] { Track } })).Started, Is.True);
        Assert.That((await player.PlayAsync()).Started, Is.True);
        Assert.That(handler.TransferCount, Is.Zero);
        Assert.That(handler.Requests.Last(r => r.Uri.AbsolutePath.EndsWith("/play")).Body, Is.Null,
            "Resume must not be converted into a request to restart a context.");
    }

    [TestCase("spotify:album:selected")]
    [TestCase(Playlist)]
    public async Task ContextStartExplicitlyRequestsFirstTrackAtZero(string contextUri)
    {
        var handler = new SpotifyHandler { ContextPlayback = true, ContextUri = contextUri };
        Assert.That((await CreatePlayer(handler).PlayAsync(body: new { context_uri = contextUri })).Started, Is.True);
        var play = handler.Requests.Single(r => r.Uri.AbsolutePath.EndsWith("/play"));
        var payload = JsonDocument.Parse(play.Body!).RootElement;
        Assert.That(payload.GetProperty("offset").GetProperty("position").GetInt32(), Is.Zero);
        Assert.That(payload.GetProperty("position_ms").GetInt32(), Is.Zero);
    }

    [Test]
    public async Task ExplicitPositionIsPreservedAndArtistDoesNotReceiveUnsupportedOffset()
    {
        var handler = new SpotifyHandler { ContextPlayback = true };
        var payload = new { context_uri = Playlist, offset = new { uri = Track }, position_ms = 42000 };
        Assert.That((await CreatePlayer(handler).PlayAsync(body: payload)).Started, Is.True);
        Assert.That(handler.Requests.Single(r => r.Uri.AbsolutePath.EndsWith("/play")).Body,
            Is.EqualTo(JsonSerializer.Serialize(payload)));

        handler = new SpotifyHandler { ContextPlayback = true, ContextUri = "spotify:artist:selected" };
        Assert.That((await CreatePlayer(handler).PlayAsync(body: new { context_uri = handler.ContextUri })).Started, Is.True);
        var artistBody = JsonDocument.Parse(handler.Requests.Single(r => r.Uri.AbsolutePath.EndsWith("/play")).Body!).RootElement;
        Assert.That(artistBody.TryGetProperty("offset", out _), Is.False);
    }

    [Test]
    public async Task FrontendShowsFailureWithoutIssuingAnotherTransfer()
    {
        var handler = new ResponseHandler(HttpStatusCode.BadGateway, "Spotify nie rozpoczęło odtwarzania.");
        var playback = new PlaybackService(new HttpClient(handler) { BaseAddress = new Uri("https://app.test/") });
        var notifications = 0;
        playback.Changed += () => notifications++;

        Assert.That(await playback.PlayAsync(new { context_uri = Playlist }), Is.False);
        Assert.That(playback.Error, Is.EqualTo("Spotify nie rozpoczęło odtwarzania."));
        Assert.That(handler.RequestCount, Is.EqualTo(1));
        Assert.That(notifications, Is.EqualTo(2));
        playback.ClearError();
        Assert.That(playback.Error, Is.Null);
    }

    private static PlayerService CreatePlayer(HttpMessageHandler handler)
    {
        var protection = new EphemeralDataProtectionProvider();
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "spotify_library_session=" + protection.CreateProtector("SpotifyBasicApi2025.TokenSession.v1").Protect(
            JsonSerializer.Serialize(new
            {
                Token = new SpotifyTokenResponse { AccessToken = "test-token" },
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                Version = 1
            }));
        var tokens = new TokenManagerService(new HttpContextAccessor { HttpContext = context }, protection);
        return new PlayerService(new ClientFactory(handler), tokens, null!);
    }

    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.spotify.com/v1/") };
    }

    private sealed record Request(HttpMethod Method, Uri Uri, string? Body);

    private sealed class SpotifyHandler : HttpMessageHandler
    {
        public List<Request> Requests { get; } = new();
        public int PlayCount { get; private set; }
        public int TransferCount { get; private set; }
        public int ActivationPolls { get; private set; }
        public int QueueCount { get; private set; }
        public bool InitiallyActive { get; init; } = true;
        public bool IgnoreFirstPlay { get; init; }
        public bool IgnoreAllPlays { get; init; }
        public bool IgnoreUrisPlayback { get; init; }
        public bool MissingAlbum { get; init; }
        public int? FailQueueAt { get; init; }
        public bool ContextPlayback { get; init; }
        public string ContextUri { get; init; } = Playlist;
        public bool Relinked { get; init; }
        public string? DevicesJson { get; init; }
        private string? playedContext;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new Request(request.Method, request.RequestUri!, request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("test-token"));
            if (request.Method == HttpMethod.Put)
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/play"))
                {
                    Assert.That(InitiallyActive || ActivationPolls >= 2, Is.True);
                    PlayCount++;
                    playedContext = null;
                    if (request.Content != null)
                    {
                        var body = JsonDocument.Parse(Requests[^1].Body!).RootElement;
                        if (body.TryGetProperty("context_uri", out var context))
                            playedContext = context.GetString();
                    }
                }
                else
                    TransferCount++;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.EndsWith("/queue"))
            {
                QueueCount++;
                return QueueCount == FailQueueAt
                    ? new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("Queue unavailable") }
                    : new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (request.RequestUri!.AbsolutePath.StartsWith("/v1/tracks/"))
                return Json(JsonSerializer.Serialize(new { uri = Relinked ? "spotify:track:replacement" : Track, album = new { uri = MissingAlbum ? null : Album } }));

            if (request.RequestUri!.AbsolutePath.EndsWith("/devices"))
                return Json(DevicesJson ?? JsonSerializer.Serialize(new { devices = new[]
                {
                    new { id = "restricted", is_active = true, is_restricted = true },
                    new { id = "inactive", is_active = false, is_restricted = true },
                    new { id = "active device", is_active = InitiallyActive, is_restricted = false }
                }}));

            if (TransferCount > 0)
                ActivationPolls++;
            var active = InitiallyActive || ActivationPolls >= 2;
            var ignored = IgnoreAllPlays || IgnoreFirstPlay && PlayCount < 2 || IgnoreUrisPlayback && playedContext == null;
            // An unrelated track can still be playing after Spotify ignores a play command.
            return Json(JsonSerializer.Serialize(new
            {
                device = new { id = "active device", is_active = active },
                is_playing = PlayCount > 0,
                item = new { uri = ignored ? "spotify:track:old" : Relinked ? "spotify:track:replacement" : Track,
                    linked_from = new { uri = Relinked && !ignored ? Track : "" } },
                context = new { uri = !ignored ? playedContext ?? (ContextPlayback ? ContextUri : "spotify:playlist:old") : "spotify:playlist:old" }
            }));
        }

        private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };
    }

    private sealed class ResponseHandler(HttpStatusCode status, string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) });
        }
    }
}
