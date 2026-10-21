using System.Reflection;
using System.Text.Json;
using Frontend.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using NUnit.Framework;
using SpotifyBasicApi2025.Controllers;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace TagDateSorting.Tests;

public class TagDateSortingTests
{
    [TestCase("album")]
    [TestCase("track")]
    [TestCase("artist")]
    public async Task EndpointsReturn400ForInvalidDateSort(string type)
    {
        await using var db = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>()
            .UseMySql("Server=localhost;Database=unused;User=test", new MySqlServerVersion(new Version(8, 0, 36))).Options);
        var protection = new EphemeralDataProtectionProvider();
        var http = new DefaultHttpContext();
        var session = new { Token = new SpotifyTokenResponse { AccessToken = "test" }, Version = 1, CachedSpotifyUserId = "me" };
        http.Request.Headers.Cookie = "spotify_library_session=" + protection.CreateProtector("SpotifyBasicApi2025.TokenSession.v1")
            .Protect(JsonSerializer.Serialize(session));
        var token = new TokenManagerService(new HttpContextAccessor { HttpContext = http }, protection);
        var users = new UserService(db, null!, token);
        var controller = new LibraryController(new LibraryService(db, users, null!, null!), null!);
        foreach (var (included, mode, tagId) in new (string?, string, long?)[]
        {
            (null, "latest", null), ("1,2", "unknown", 1), ("1,2", "single", null), ("1,2", "single", 3)
        })
        {
            var result = type switch
            {
                "album" => await controller.GetUserAlbums(sort: "tagAdded", includeTags: included, tagDateMode: mode, sortTagId: tagId),
                "track" => await controller.GetUserTracks(sort: "tagAdded", includeTags: included, tagDateMode: mode, sortTagId: tagId),
                _ => await controller.GetUserArtists(sort: "tagAdded", includeTags: included, tagDateMode: mode, sortTagId: tagId)
            };
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>(), $"{type}, {included}, {mode}, {tagId}");
        }
    }

    [Test]
    public void ResolvesOnlyPositiveFiltersAndValidatesSelection()
    {
        Assert.That(TagAssignmentDateSorting.ResolveTagIds("3,1", "2,1", null, "latest"), Is.EqualTo(new long[] { 1, 2, 3 }));
        Assert.That(TagAssignmentDateSorting.ResolveTagIds("3", "2,1", 2, null), Is.EqualTo(new long[] { 2 }));
        Assert.Throws<ArgumentException>(() => TagAssignmentDateSorting.ResolveTagIds(null, null, 1, "latest"));
        Assert.Throws<ArgumentException>(() => TagAssignmentDateSorting.ResolveTagIds(null, "1,2", null, "single"));
        Assert.Throws<ArgumentException>(() => TagAssignmentDateSorting.ResolveTagIds(null, "1,2", 3, "single"));
        Assert.Throws<ArgumentException>(() => TagAssignmentDateSorting.ResolveTagIds(null, "1", 1, "unknown"));
        Assert.Throws<ArgumentException>(() => TagAssignmentDateSorting.ResolveTagIds(null, "x", null, "latest"));
    }

    [TestCase(false, "b,a,c,d,e")]
    [TestCase(true, "a,c,d,b,e")]
    public async Task LatestDateOrdersBeforePagingAndIgnoresOtherAssignments(bool descending, string expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new SortTestContext(new DbContextOptionsBuilder<SortTestContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);
        var query = TagAssignmentDateSorting.Apply(db.Items, db.Assignments, "me", "album", new long[] { 1, 2 }, descending);
        var ids = await query.Select(item => item.Id).ToListAsync();
        Assert.That(string.Join(',', ids), Is.EqualTo(expected));
        var paged = new List<string>();
        for (var offset = 0; offset < ids.Count; offset += 2)
            paged.AddRange(await query.Skip(offset).Take(2).Select(item => item.Id).ToListAsync());
        Assert.That(paged, Is.EqualTo(ids));
    }

    [Test]
    public async Task SingleTagUsesCreatedAtAndReassignmentGetsNewDate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new SortTestContext(new DbContextOptionsBuilder<SortTestContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);
        IQueryable<SortItem> Query() => TagAssignmentDateSorting.Apply(db.Items, db.Assignments, "me", "album", new long[] { 1 }, true);
        var original = await Query().Select(item => item.Id).ToListAsync();
        Assert.That(original, Is.EqualTo(new[] { "b", "a", "e", "c", "d" }));
        var assignment = await db.Assignments.SingleAsync(a => a.UserId == "me" && a.ItemType == "album" && a.SpotifyItemId == "a" && a.TagId == 1);
        assignment.Strength = 6;
        assignment.UpdatedAt = new DateTime(2026, 10, 25);
        await db.SaveChangesAsync();
        Assert.That(await Query().Select(item => item.Id).ToListAsync(), Is.EqualTo(original));
        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync();
        db.Assignments.Add(Assignment("a", 1, 26));
        await db.SaveChangesAsync();
        Assert.That(await Query().Select(item => item.Id).FirstAsync(), Is.EqualTo("a"));
    }

    [TestCase("album", false)]
    [TestCase("album", true)]
    [TestCase("track", false)]
    [TestCase("track", true)]
    [TestCase("artist", false)]
    [TestCase("artist", true)]
    public void MySqlTranslatesProductionEntitiesAndPaging(string type, bool descending)
    {
        using var db = new DatabaseContext(new DbContextOptionsBuilder<DatabaseContext>()
            .UseMySql("Server=localhost;Database=sql_translation;User=test", new MySqlServerVersion(new Version(8, 0, 36))).Options);
        string Sql<T>(IQueryable<T> source) where T : class => TagAssignmentDateSorting
            .Apply(source, db.LibTagAssigns, "me", type, new long[] { 1, 2 }, descending).Skip(2).Take(2).ToQueryString();
        var sql = type switch
        {
            "album" => Sql(db.LibAlbumUserAssigns.Where(a => a.UserId == "me").Select(a => a.Album)),
            "track" => Sql(db.LibTrackUserAssigns.Where(a => a.UserId == "me")
                .Include(a => a.Track).ThenInclude(t => t.Album)
                .Include(a => a.Track).ThenInclude(t => t.TrackArtists).ThenInclude(a => a.Artist).Select(a => a.Track)),
            _ => Sql(db.LibArtistUserAssigns.Where(a => a.UserId == "me").Select(a => a.Artist))
        };
        Assert.That(sql, Does.Contain("MAX(").And.Contain("CreatedAt").And.Contain("IS NULL").And.Contain("ORDER BY").And.Contain("LIMIT"));
        Assert.That(sql, Does.Not.Contain("UpdatedAt` DESC"));
    }

    [TestCase(typeof(Frontend.Pages.Albums))]
    [TestCase(typeof(Frontend.Pages.Tracks))]
    [TestCase(typeof(Frontend.Pages.Artists))]
    public void PagesNormalizeSelectionAndPersistItInStateAndSnapshot(Type pageType)
    {
        var page = Activator.CreateInstance(pageType)!;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Field(string name, object? value) => pageType.GetField(name, flags)!.SetValue(page, value);
        object? Read(string name) => pageType.GetField(name, flags)!.GetValue(page);
        void Property(string name, object? value) => pageType.GetProperty(name, flags)!.SetValue(page, value);
        object Capture() => pageType.GetMethod("CaptureCurrentFilters", flags)!.Invoke(page, null)!;
        Field("includeTagIds", new HashSet<long> { 1 });
        Property("SortKeySelection", "tagAdded");
        Assert.That(Read("selectedTagSort"), Is.EqualTo("1"));
        Assert.That(pageType.GetProperty("sortOrder", flags)?.GetValue(page) ?? Read("sortOrder"), Is.EqualTo("desc"));
        Field("includeTagIds", new HashSet<long> { 2, 3 });
        Capture();
        Assert.That(Read("selectedTagSort"), Is.Null);
        Assert.That(pageType.GetProperty("CanApplyTagDateSort", flags)!.GetValue(page), Is.False);
        Property("TagDateSelection", "latest");
        var snapshot = Capture();
        Assert.That(snapshot.GetType().GetProperty("TagDateMode")!.GetValue(snapshot), Is.EqualTo("latest"));
        var state = pageType.GetMethod("BuildCurrentState", flags)!.Invoke(page, null)!;
        var json = JsonSerializer.Serialize(state, state.GetType());
        Assert.That(json, Does.Contain("\"tagDateMode\":\"latest\""));
        var restored = JsonSerializer.Deserialize(json, state.GetType())!;
        Assert.That(restored.GetType().GetProperty("tagDateMode")!.GetValue(restored), Is.EqualTo("latest"));
        Field("includeTagIds", new HashSet<long> { 3 });
        Capture();
        Assert.That(Read("tagDateMode"), Is.EqualTo("latest"));
        Field("includeTagIds", new HashSet<long>());
        var cleared = Capture();
        Assert.That(cleared.GetType().GetProperty("SortKey")!.GetValue(cleared), Is.EqualTo("name"));
    }

    [Test]
    public void LatestQueryOmitsStaleSingleTagAndWeightSortIsUnchanged()
    {
        var query = new List<string>();
        TagDateSortSelection.AppendQuery(query, "tagAdded", "latest", "12");
        Assert.That(query, Is.EqualTo(new[] { "tagDateMode=latest" }));
        query.Clear();
        TagDateSortSelection.AppendQuery(query, "weight", "latest", "12");
        Assert.That(query, Is.EqualTo(new[] { "sortTagId=12" }));
        Assert.That(TagDateSortSelection.Candidates(new long[] { 1, 2 }, "3"), Is.EqualTo(new long[] { 1, 2, 3 }));
    }

    [Test]
    public async Task SelectorShowsAggregateAndSeparateTags()
    {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<TagDateSortSelect>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Value"] = "latest",
                ["Tags"] = new List<TagItem> { new() { Id = 1, Name = "my library" }, new() { Id = 2, Name = "metal" } }
            }));
            return component.ToHtmlString();
        });
        Assert.That(html, Does.Match("<option value=\"latest\"[^>]* selected>").And.Contain("my library").And.Contain("metal"));
    }

    [Test]
    public async Task MySqlExecutionMatchesDateAndPagingScenarios()
    {
        var configured = Environment.GetEnvironmentVariable("SPOTIFY_TEST_MYSQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Set SPOTIFY_TEST_MYSQL_CONNECTION for a local test server; the test creates and removes its own database.");

        var connectionString = new MySqlConnectionStringBuilder(configured!) { Database = "", Pooling = false };
        await using var admin = new MySqlConnection(connectionString.ConnectionString);
        await admin.OpenAsync();
        var database = "codex_tagdate_" + Guid.NewGuid().ToString("N");
        await using (var create = new MySqlCommand($"CREATE DATABASE `{database}`", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            connectionString.Database = database;
            await using var db = new SortTestContext(new DbContextOptionsBuilder<SortTestContext>()
                .UseMySql(connectionString.ConnectionString, new MySqlServerVersion(new Version(8, 0, 36))).Options);
            await db.Database.EnsureCreatedAsync();
            await SeedAsync(db);
            var latest = TagAssignmentDateSorting.Apply(db.Items, db.Assignments, "me", "album", new long[] { 1, 2 }, true);
            Assert.That(await latest.Select(item => item.Id).ToListAsync(), Is.EqualTo(new[] { "a", "c", "d", "b", "e" }));
            Assert.That(await latest.Skip(2).Take(2).Select(item => item.Id).ToListAsync(), Is.EqualTo(new[] { "d", "b" }));
            var oldest = TagAssignmentDateSorting.Apply(db.Items, db.Assignments, "me", "album", new long[] { 1, 2 }, false);
            Assert.That(await oldest.Select(item => item.Id).ToListAsync(), Is.EqualTo(new[] { "b", "a", "c", "d", "e" }));
            var single = TagAssignmentDateSorting.Apply(db.Items, db.Assignments, "me", "album", new long[] { 1 }, true);
            Assert.That(await single.Select(item => item.Id).ToListAsync(), Is.EqualTo(new[] { "b", "a", "e", "c", "d" }));
        }
        finally
        {
            await using var drop = new MySqlCommand($"DROP DATABASE `{database}`", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static LibTagAssign Assignment(string item, long tag, int day, string user = "me", string type = "album") => new()
    {
        SpotifyItemId = item, TagId = tag, UserId = user, ItemType = type,
        CreatedAt = new DateTime(2026, 10, day), UpdatedAt = new DateTime(2026, 10, day)
    };

    private static async Task SeedAsync(SortTestContext db)
    {
        db.Items.AddRange(new SortItem("a", "Alpha"), new SortItem("b", "Beta"), new SortItem("c", "Same"), new SortItem("d", "Same"), new SortItem("e", "Missing"));
        db.Assignments.AddRange(Assignment("a", 1, 1), Assignment("a", 2, 20), Assignment("b", 1, 15),
            Assignment("c", 2, 20), Assignment("d", 2, 20), Assignment("e", 2, 30, "other"),
            Assignment("e", 2, 29, type: "artist"), Assignment("e", 99, 28));
        await db.SaveChangesAsync();
    }

    public class SortItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public SortItem() { }
        public SortItem(string id, string name) { Id = id; Name = name; }
    }

    public class SortTestContext(DbContextOptions<SortTestContext> options) : DbContext(options)
    {
        public DbSet<SortItem> Items => Set<SortItem>();
        public DbSet<LibTagAssign> Assignments => Set<LibTagAssign>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SortItem>();
            modelBuilder.Entity<LibTagAssign>().Ignore(a => a.User).Ignore(a => a.Tag);
        }
    }
}
