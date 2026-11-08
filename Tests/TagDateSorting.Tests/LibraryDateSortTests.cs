using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SpotifyBasicApi2025.Controllers;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace TagDateSorting.Tests;

public class LibraryDateSortTests
{
    [TestCase("album")]
    [TestCase("track")]
    [TestCase("artist")]
    public async Task EndpointsCombineDateSortWithExistingFilters(string type)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite(connection, options => options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options);
        await db.Database.EnsureCreatedAsync();
        db.SpUsers.Add(new SpUser { Id = "me" });
        db.LibTagGroups.Add(new LibTagGroup { Id = 1, Name = "Tags", UserId = "me" });
        db.LibTags.AddRange(new LibTag { Id = 1, Name = "my library", TagGroupId = 1 }, new LibTag { Id = 2, Name = "metal", TagGroupId = 1 });
        foreach (var (id, name) in new[] { ("a", "Alpha"), ("b", "Beta"), ("c", "Same"), ("d", "Same"), ("e", "Missing") })
        {
            db.SpAlbums.Add(new SpAlbum { Id = id, Name = name });
            db.SpTracks.Add(new SpTrack { Id = id, Name = name });
            db.SpArtists.Add(new SpArtist { Id = id, Name = name });
            db.LibAlbumUserAssigns.Add(new LibAlbumUserAssign { UserId = "me", AlbumId = id });
            db.LibTrackUserAssigns.Add(new LibTrackUserAssign { UserId = "me", TrackId = id });
            db.LibArtistUserAssigns.Add(new LibArtistUserAssign { UserId = "me", ArtistId = id });
        }
        foreach (var itemType in new[] { "album", "track", "artist" })
        foreach (var (id, tag, day, strength) in new[] { ("a", 1, 1, 3f), ("a", 2, 20, 3f), ("b", 1, 15, 5f), ("c", 2, 20, 5f), ("d", 2, 20, 3f) })
            db.LibTagAssigns.Add(new LibTagAssign { UserId = "me", ItemType = itemType, SpotifyItemId = id, TagId = tag, Strength = strength, CreatedAt = new DateTime(2026, 10, day) });
        db.SpAlbumArtistAssigns.Add(new SpAlbumArtistAssign { AlbumId = "e", ArtistId = "c", AlbumName = "Missing", ArtistName = "Same" });
        await db.SaveChangesAsync();

        var protection = new EphemeralDataProtectionProvider();
        var http = new DefaultHttpContext();
        http.Request.Headers.Cookie = "spotify_library_session=" + protection.CreateProtector("SpotifyBasicApi2025.TokenSession.v1").Protect(
            JsonSerializer.Serialize(new { Token = new SpotifyTokenResponse { AccessToken = "test" }, Version = 1, CachedSpotifyUserId = "me" }));
        var users = new UserService(db, null!, new TokenManagerService(new HttpContextAccessor { HttpContext = http }, protection));
        var controller = new LibraryController(new LibraryService(db, users, null!, null!), null!);

        async Task<string[]> Get(string order = "desc", string mode = "latest", long? tag = null,
            string includeMode = "or", string? strengths = null, string? excluded = null, bool inherited = false, int page = 1, int pageSize = 100,
            string? excludedStrengths = null, string excludeMode = "or")
        {
            var result = type switch
            {
                "album" => await controller.GetUserAlbums(sort: "tagAdded", order: order, tagDateMode: mode, sortTagId: tag,
                    includeTags: "1,2", includeMode: includeMode, includeTagStrengths: strengths, excludeTags: excluded,
                    includeArtistTags: inherited, page: page, pageSize: pageSize, excludeTagStrengths: excludedStrengths, excludeMode: excludeMode),
                "track" => await controller.GetUserTracks(sort: "tagAdded", order: order, tagDateMode: mode, sortTagId: tag,
                    includeTags: "1,2", includeMode: includeMode, includeTagStrengths: strengths, excludeTags: excluded, page: page, pageSize: pageSize,
                    excludeTagStrengths: excludedStrengths, excludeMode: excludeMode),
                _ => await controller.GetUserArtists(sort: "tagAdded", order: order, tagDateMode: mode, sortTagId: tag,
                    includeTags: "1,2", includeMode: includeMode, includeTagStrengths: strengths, excludeTags: excluded,
                    excludeTagStrengths: excludedStrengths, excludeMode: excludeMode)
            };
            Assert.That(result, Is.InstanceOf<OkObjectResult>(), JsonSerializer.Serialize((result as ObjectResult)?.Value));
            var json = JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
            return json.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("Id").GetString()!).ToArray();
        }

        Assert.That(await Get(), Is.EqualTo(new[] { "a", "c", "d", "b" }));
        Assert.That(await Get(order: "asc"), Is.EqualTo(new[] { "b", "a", "c", "d" }));
        Assert.That(await Get(mode: "single", tag: 1), Is.EqualTo(new[] { "b", "a", "c", "d" }));
        Assert.That(await Get(includeMode: "and"), Is.EqualTo(new[] { "a" }));
        Assert.That(await Get(strengths: "1:gte:4,2:gte:4"), Is.EqualTo(new[] { "c", "b" }));
        Assert.That(await Get(excluded: "1"), Is.EqualTo(new[] { "c", "d" }));
        Assert.That(await Get(excluded: "1", excludedStrengths: "1:gte:4"), Is.EqualTo(new[] { "a", "c", "d" }));
        Assert.That(await Get(excluded: "1,2", excludeMode: "and", excludedStrengths: "1:gte:4,2:gte:4"), Is.EqualTo(new[] { "a", "c", "d", "b" }));
        if (type != "artist")
        {
            Assert.That(await Get(pageSize: 2), Is.EqualTo(new[] { "a", "c" }));
            Assert.That(await Get(page: 2, pageSize: 2), Is.EqualTo(new[] { "d", "b" }));
        }
        if (type == "album")
        {
            Assert.That(await Get(inherited: true), Is.EqualTo(new[] { "a", "c", "d", "b", "e" }));
            Assert.That(await Get(inherited: true, order: "asc"), Is.EqualTo(new[] { "b", "a", "c", "d", "e" }));
            Assert.That(await Get(inherited: true, excluded: "2", excludedStrengths: "2:gte:4"), Is.EqualTo(new[] { "a", "d", "b" }));
        }

        var editedAssignment = await db.LibTagAssigns.SingleAsync(assign => assign.ItemType == type && assign.SpotifyItemId == "a" && assign.TagId == 1);
        editedAssignment.Strength = 4;
        await db.SaveChangesAsync();
        Assert.That(await Get(excluded: "1", excludedStrengths: "1:gte:4"), Is.EqualTo(new[] { "c", "d" }));
    }
}
