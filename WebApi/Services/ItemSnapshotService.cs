using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Enums;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services;

public sealed class ItemSnapshotService(DatabaseContext context)
{
    public async Task UpsertAsync(string userId, string itemType, string spotifyItemId, LibItemSnapshotInput snapshot)
    {
        var normalizedType = itemType.Trim().ToLowerInvariant();
        if (!SpotifyItemType.IsValid(normalizedType) || string.IsNullOrWhiteSpace(spotifyItemId))
            throw new ArgumentException("Nieprawidłowy typ lub identyfikator elementu.");
        if (string.IsNullOrWhiteSpace(snapshot.Title))
            throw new ArgumentException("Opis archiwalny musi zawierać tytuł elementu.");

        var entity = await context.LibItemSnapshots.SingleOrDefaultAsync(x =>
            x.UserId == userId && x.ItemType == normalizedType && x.SpotifyItemId == spotifyItemId);
        var now = DateTime.UtcNow;
        if (entity == null)
        {
            entity = new LibItemSnapshot { UserId = userId, ItemType = normalizedType, SpotifyItemId = spotifyItemId, CapturedAtUtc = now };
            context.LibItemSnapshots.Add(entity);
        }

        entity.Title = snapshot.Title.Trim();
        entity.ArtistsJson = JsonSerializer.Serialize((snapshot.Artists ?? new()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct());
        entity.AlbumTitle = NullIfWhiteSpace(snapshot.AlbumTitle);
        entity.OwnerName = NullIfWhiteSpace(snapshot.OwnerName);
        entity.SpotifyUri = NullIfWhiteSpace(snapshot.SpotifyUri);
        entity.LastConfirmedAtUtc = now;
    }

    public async Task DeleteIfOrphanedAsync(string userId, string itemType, string spotifyItemId)
    {
        var hasTag = await context.LibTagAssigns.AnyAsync(x => x.UserId == userId && x.ItemType == itemType && x.SpotifyItemId == spotifyItemId);
        var hasNote = await context.LibItemNotes.AnyAsync(x => x.UserId == userId && x.ItemType == itemType && x.SpotifyItemId == spotifyItemId);
        if (hasTag || hasNote) return;
        var snapshot = await context.LibItemSnapshots.SingleOrDefaultAsync(x => x.UserId == userId && x.ItemType == itemType && x.SpotifyItemId == spotifyItemId);
        if (snapshot != null) context.LibItemSnapshots.Remove(snapshot);
    }

    public static List<string> ParseArtists(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
