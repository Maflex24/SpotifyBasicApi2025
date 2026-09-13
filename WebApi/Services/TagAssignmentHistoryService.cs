using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models.Extensions;

namespace SpotifyBasicApi2025.Services
{
    public class TagAssignmentHistoryService
    {
        private readonly DatabaseContext context;

        public TagAssignmentHistoryService(DatabaseContext context)
        {
            this.context = context;
        }

        /// <summary>Adds an audit entry to the current DbContext unit of work without saving it.</summary>
        public async Task RecordAsync(
            string userId,
            LibTagAssign assignment,
            string action,
            float? previousStrength,
            float? newStrength,
            DateTime? changedAt = null)
        {
            var tag = await context.LibTags
                .AsNoTracking()
                .Where(item => item.Id == assignment.TagId)
                .Select(item => new { item.Name, GroupName = item.TagGroup.Name })
                .FirstOrDefaultAsync();

            var item = await GetItemSnapshotAsync(assignment.ItemType, assignment.SpotifyItemId);

            context.LibTagAssignmentHistories.Add(new LibTagAssignmentHistory
            {
                UserId = userId,
                Action = action,
                TagId = assignment.TagId,
                TagName = tag?.Name ?? $"Tag #{assignment.TagId}",
                TagGroupName = tag?.GroupName ?? "Usunięta grupa",
                ItemType = assignment.ItemType,
                SpotifyItemId = assignment.SpotifyItemId,
                ItemName = item.Name,
                ImageUrl = SelectSmallestImageUrl(item.Images) ?? SelectSmallestImageUrl(item.FallbackImages),
                PreviousStrength = previousStrength,
                NewStrength = newStrength,
                ChangedAt = changedAt ?? DateTime.UtcNow
            });
        }

        private async Task<ItemSnapshot> GetItemSnapshotAsync(string itemType, string spotifyItemId)
        {
            var item = itemType.ToLowerInvariant() switch
            {
                "artist" => await context.SpArtists.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => new ItemSnapshot(item.Name, item.Images, null)).FirstOrDefaultAsync(),
                "album" => await context.SpAlbums.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => new ItemSnapshot(item.Name, item.Images, null)).FirstOrDefaultAsync(),
                "track" => await context.SpTracks.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => new ItemSnapshot(item.Name, item.Album == null ? null : item.Album.Images, item.Images)).FirstOrDefaultAsync(),
                "playlist" => await context.SpPlaylists.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => new ItemSnapshot(item.Name, item.Images, null)).FirstOrDefaultAsync(),
                _ => null
            };

            return item ?? new ItemSnapshot(spotifyItemId, null, null);
        }

        private static string? SelectSmallestImageUrl(string? images) => ImageMappingExtensions.ParseSpotifyImages(images)
            .Where(image => !string.IsNullOrWhiteSpace(image.Url))
            .OrderBy(image => image.Width ?? int.MaxValue)
            .ThenBy(image => image.Height ?? int.MaxValue)
            .Select(image => image.Url)
            .FirstOrDefault();

        private sealed record ItemSnapshot(string Name, string? Images, string? FallbackImages);
    }
}
