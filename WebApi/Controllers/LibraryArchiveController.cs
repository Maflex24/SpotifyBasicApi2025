using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers;

[ApiController]
[Route("api/library/archive")]
public sealed class LibraryArchiveController(DatabaseContext context, LibraryOwnerService ownerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LibItemSnapshotModel>>> Get([FromQuery] string? itemType = null)
    {
        var owner = await ownerService.RequireOwnerSpotifyUserIdAsync();
        var normalizedType = itemType?.Trim().ToLowerInvariant();
        var query = context.LibItemSnapshots.AsNoTracking().Where(x => x.UserId == owner);
        if (!string.IsNullOrWhiteSpace(normalizedType)) query = query.Where(x => x.ItemType == normalizedType);

        var snapshots = await query.OrderBy(x => x.ItemType).ThenBy(x => x.ArtistsJson).ThenBy(x => x.Title).ToListAsync();
        var tags = await context.LibTagAssigns.AsNoTracking()
            .Where(x => x.UserId == owner)
            .Select(x => new { x.ItemType, x.SpotifyItemId, x.TagId, TagName = x.Tag.Name, TagGroupName = x.Tag.TagGroup.Name, x.Strength })
            .ToListAsync();
        var notes = await context.LibItemNotes.AsNoTracking().Where(x => x.UserId == owner)
            .ToDictionaryAsync(x => x.ItemType + "\u001f" + x.SpotifyItemId, x => x.Content);

        var snapshotKeys = snapshots.Select(x => x.ItemType + "\u001f" + x.SpotifyItemId).ToHashSet();
        return Ok(snapshots.Select(snapshot => new LibItemSnapshotModel
        {
            ItemType = snapshot.ItemType,
            SpotifyItemId = snapshot.SpotifyItemId,
            Title = snapshot.Title,
            Artists = ItemSnapshotService.ParseArtists(snapshot.ArtistsJson),
            AlbumTitle = snapshot.AlbumTitle,
            OwnerName = snapshot.OwnerName,
            SpotifyUri = snapshot.SpotifyUri,
            CapturedAtUtc = snapshot.CapturedAtUtc,
            LastConfirmedAtUtc = snapshot.LastConfirmedAtUtc,
            Note = notes.GetValueOrDefault(snapshot.ItemType + "\u001f" + snapshot.SpotifyItemId),
            Tags = tags.Where(tag => snapshotKeys.Contains(tag.ItemType + "\u001f" + tag.SpotifyItemId) && tag.ItemType == snapshot.ItemType && tag.SpotifyItemId == snapshot.SpotifyItemId)
                .Select(tag => new TagAssignmentStateModel { TagId = tag.TagId, TagName = tag.TagName, TagGroupName = tag.TagGroupName, Strength = tag.Strength }).ToList()
        }).ToList());
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var result = await Get();
        if (result.Result is ObjectResult { Value: List<LibItemSnapshotModel> snapshots })
            return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(snapshots, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }), "application/json", "spotify-library-archive.json");
        return result.Result ?? StatusCode(500);
    }
}
