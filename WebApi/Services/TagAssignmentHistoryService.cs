using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;

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

            context.LibTagAssignmentHistories.Add(new LibTagAssignmentHistory
            {
                UserId = userId,
                Action = action,
                TagId = assignment.TagId,
                TagName = tag?.Name ?? $"Tag #{assignment.TagId}",
                TagGroupName = tag?.GroupName ?? "Usunięta grupa",
                ItemType = assignment.ItemType,
                SpotifyItemId = assignment.SpotifyItemId,
                ItemName = await GetItemNameAsync(assignment.ItemType, assignment.SpotifyItemId),
                PreviousStrength = previousStrength,
                NewStrength = newStrength,
                ChangedAt = changedAt ?? DateTime.UtcNow
            });
        }

        private async Task<string> GetItemNameAsync(string itemType, string spotifyItemId)
        {
            var name = itemType.ToLowerInvariant() switch
            {
                "artist" => await context.SpArtists.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => item.Name).FirstOrDefaultAsync(),
                "album" => await context.SpAlbums.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => item.Name).FirstOrDefaultAsync(),
                "track" => await context.SpTracks.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => item.Name).FirstOrDefaultAsync(),
                "playlist" => await context.SpPlaylists.AsNoTracking().Where(item => item.Id == spotifyItemId).Select(item => item.Name).FirstOrDefaultAsync(),
                _ => null
            };

            return string.IsNullOrWhiteSpace(name) ? spotifyItemId : name;
        }
    }
}
