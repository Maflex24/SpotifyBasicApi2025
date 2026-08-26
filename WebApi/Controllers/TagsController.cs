using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly LibraryOwnerService ownerService;
        private readonly ItemSnapshotService snapshotService;
        private readonly DatabaseContext dbContext;

        public TagsController(TagsService tagsService, SpotifyApiService spotifyApiService, LibraryOwnerService ownerService, ItemSnapshotService snapshotService, DatabaseContext dbContext)
        {
            this.tagsService = tagsService;
            this.spotifyApiService = spotifyApiService;
            this.ownerService = ownerService;
            this.snapshotService = snapshotService;
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            try
            {
                var tags = await tagsService.GetAllTagsAsync(userId);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagów: {ex.Message}");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetTagsList(
            [FromQuery] string categoriesIds)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            try
            {
                var tags = await tagsService.GetTagsList(userId, categoriesIds);

                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagów: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagById(int id)
        {
            try
            {
                var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();
                var tag = await tagsService.GetTagByIdAsync(id, userId);

                if (tag == null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania tagu: {ex.Message}");
            }
        }

        public sealed class PinTagRequest
        {
            public bool IsPinned { get; set; }
        }

        [HttpPut("{id}/pin")]
        public async Task<IActionResult> SetTagPinned(long id, [FromBody] PinTagRequest request)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            var updated = await tagsService.SetTagPinnedAsync(id, request.IsPinned, userId);
            return updated ? NoContent() : NotFound($"Tag o ID {id} nie został znaleziony");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();

                if (!await tagsService.UserOwnsTagGroupAsync(tagModel.TagGroupId, userId))
                    return BadRequest("Wybrana grupa tagów nie istnieje lub nie należy do użytkownika");

                var createdTag = await tagsService.CreateTagAsync(tagModel, userId);

                return CreatedAtAction(nameof(GetTagById), new { id = createdTag.Id }, createdTag);
            }
            catch (TagsService.TagNameValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas tworzenia tagu: {ex.Message}");
            }
        }

        // --- Assigns ---

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTag([FromBody] TagAssignModel model)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            model.ItemType = model.ItemType.Trim().ToLowerInvariant();
            if (model.TagId <= 0 || string.IsNullOrWhiteSpace(model.ItemType) || string.IsNullOrWhiteSpace(model.SpotifyItemId))
                return BadRequest("Nieprawidłowe dane przypisania tagu");
            model.Snapshot ??= await ResolveSnapshotAsync(model.ItemType, model.SpotifyItemId);
            if (model.Snapshot is null) return BadRequest("Nie udało się pobrać opisu elementu ze Spotify.");

            var existing = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            var strength = Math.Clamp(model.Strength, 0.0f, 6.0f);

            if (existing is not null)
            {
                existing.Strength = strength;
                existing.UpdatedAt = DateTime.UtcNow;
                await snapshotService.UpsertAsync(userId, model.ItemType, model.SpotifyItemId, model.Snapshot);
                await dbContext.SaveChangesAsync();
                return NoContent();
            }

            var entity = new LibTagAssign
            {
                TagId = model.TagId,
                UserId = userId,
                SpotifyItemId = model.SpotifyItemId,
                ItemType = model.ItemType,
                Strength = strength,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.LibTagAssigns.Add(entity);
            await snapshotService.UpsertAsync(userId, model.ItemType, model.SpotifyItemId, model.Snapshot);
            await dbContext.SaveChangesAsync();

            return Created($"api/tags/assign/{entity.Id}", entity.Id);
        }

        [HttpPut("assign")]
        public async Task<IActionResult> UpdateAssign([FromBody] TagAssignModel model)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            model.ItemType = model.ItemType.Trim().ToLowerInvariant();
            var entity = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            var strength = Math.Clamp(model.Strength, 0.0f, 6.0f);

            if (entity is null)
            {
                // Upsert: utwórz jeśli nie istnieje
                model.Snapshot ??= await ResolveSnapshotAsync(model.ItemType, model.SpotifyItemId);
                if (model.Snapshot is null) return BadRequest("Nie udało się pobrać opisu elementu ze Spotify.");
                var created = new LibTagAssign
                {
                    TagId = model.TagId,
                    UserId = userId,
                    SpotifyItemId = model.SpotifyItemId,
                    ItemType = model.ItemType,
                    Strength = strength,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.LibTagAssigns.Add(created);
                await snapshotService.UpsertAsync(userId, model.ItemType, model.SpotifyItemId, model.Snapshot);
                await dbContext.SaveChangesAsync();
                return NoContent();
            }

            entity.Strength = strength;
            entity.UpdatedAt = DateTime.UtcNow;
            if (model.Snapshot is not null)
                await snapshotService.UpsertAsync(userId, model.ItemType, model.SpotifyItemId, model.Snapshot);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("assign")]
        public async Task<IActionResult> RemoveAssign([FromBody] TagAssignModel model)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            var entity = await dbContext.LibTagAssigns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TagId == model.TagId && a.SpotifyItemId == model.SpotifyItemId && a.ItemType == model.ItemType);

            if (entity is null)
                return NotFound();

            dbContext.LibTagAssigns.Remove(entity);
            await dbContext.SaveChangesAsync();
            await snapshotService.DeleteIfOrphanedAsync(userId, entity.ItemType, entity.SpotifyItemId);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("assign/{itemType}/{spotifyItemId}")]
        public async Task<IActionResult> GetAssignsForItem([FromRoute] string itemType, [FromRoute] string spotifyItemId)
        {
            string userId;
            try { userId = await ownerService.RequireOwnerSpotifyUserIdAsync(); }
            catch (Exception ex) { return Unauthorized(ex.Message); }

            var normalizedItemType = itemType.Trim().ToLowerInvariant();
            if (normalizedItemType is not ("artist" or "album" or "track" or "playlist") || string.IsNullOrWhiteSpace(spotifyItemId))
                return BadRequest("Nieprawidłowy typ lub identyfikator elementu");

            var assigns = await dbContext.LibTagAssigns
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.ItemType == normalizedItemType && a.SpotifyItemId == spotifyItemId)
                .OrderBy(a => a.Tag.Name)
                .Select(a => new TagAssignmentStateModel
                {
                    TagId = a.TagId,
                    TagName = a.Tag.Name,
                    TagGroupName = a.Tag.TagGroup.Name,
                    Strength = a.Strength
                })
                .ToListAsync();

            return Ok(assigns);
        }

        private async Task<LibItemSnapshotInput?> ResolveSnapshotAsync(string itemType, string spotifyItemId)
        {
            switch (itemType)
            {
                case "artist":
                    var artist = await spotifyApiService.GetArtist(spotifyItemId);
                    return artist == null ? null : new LibItemSnapshotInput { Title = artist.Name, Artists = new List<string> { artist.Name }, SpotifyUri = artist.Uri };
                case "album":
                    var album = await spotifyApiService.GetAlbumAsync(spotifyItemId);
                    return album == null ? null : new LibItemSnapshotInput { Title = album.Name, Artists = album.Artists.Select(x => x.Name).ToList(), SpotifyUri = album.Uri };
                case "track":
                    var track = await spotifyApiService.GetTrackAsync(spotifyItemId);
                    return track == null ? null : new LibItemSnapshotInput { Title = track.Name, Artists = track.Artists.Select(x => x.Name).ToList(), AlbumTitle = track.Album?.Name, SpotifyUri = track.Uri };
                case "playlist":
                    var playlist = await spotifyApiService.GetPlaylistAsync(spotifyItemId);
                    return playlist == null ? null : new LibItemSnapshotInput { Title = playlist.Name, OwnerName = playlist.Owner?.DisplayName, SpotifyUri = playlist.Uri };
                default:
                    return null;
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagModel tagModel)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();

                if (!await tagsService.UserOwnsTagGroupAsync(tagModel.TagGroupId, userId))
                    return BadRequest("Wybrana grupa tagów nie istnieje lub nie należy do użytkownika");

                var updatedTag = await tagsService.UpdateTagAsync(id, tagModel, userId);

                if (updatedTag == null)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return Ok(updatedTag);
            }
            catch (TagsService.TagNameValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas aktualizacji tagu: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var userId = await ownerService.RequireOwnerSpotifyUserIdAsync();
                var success = await tagsService.DeleteTagAsync(id, userId);

                if (!success)
                    return NotFound($"Tag o ID {id} nie został znaleziony");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania tagu: {ex.Message}");
            }
        }
    }
}
