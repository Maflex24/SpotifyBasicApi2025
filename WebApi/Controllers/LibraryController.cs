using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Models;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/library")]
    public class LibraryController : ControllerBase
    {
        private readonly LibraryService libraryService;
        private readonly SpotifyApiService spotifyApiService;
        private readonly LiveLibraryService liveLibraryService;
        private readonly LibraryOwnerService ownerService;
        

        public LibraryController(LibraryService libraryService, SpotifyApiService spotifyApiService, LiveLibraryService liveLibraryService, LibraryOwnerService ownerService)
        {
            this.libraryService = libraryService;
            this.spotifyApiService = spotifyApiService;
            this.liveLibraryService = liveLibraryService;
            this.ownerService = ownerService;
        }

        private async Task InvalidateLiveLibraryAsync()
        {
            try { spotifyApiService.InvalidateLiveLibrary(await ownerService.RequireOwnerSpotifyUserIdAsync()); }
            catch { }
        }
        [HttpGet("filters")] // GET api/library/filters?scope=tracks&contextId=...
        public async Task<IActionResult> GetSavedFilters([FromQuery] string scope, [FromQuery] string? contextId = null)
        {
            try
            {
                var result = await libraryService.GetSavedFiltersAsync(scope, contextId);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        public sealed class SaveFilterModel { public string Scope { get; set; } = string.Empty; public string? ContextId { get; set; } public string Name { get; set; } = string.Empty; public string PayloadJson { get; set; } = string.Empty; }

        public sealed class ItemNoteRequest
        {
            public string ItemType { get; set; } = string.Empty;
            public string SpotifyItemId { get; set; } = string.Empty;
            public string? Content { get; set; }
            public LibItemSnapshotInput? Snapshot { get; set; }
        }

        [HttpGet("notes/{itemType}/{spotifyItemId}")]
        public async Task<IActionResult> GetItemNote(string itemType, string spotifyItemId)
        {
            try
            {
                var note = await libraryService.GetItemNoteAsync(itemType, spotifyItemId);
                return note == null ? NotFound() : Ok(note);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPut("notes")]
        public async Task<IActionResult> SetItemNote([FromBody] ItemNoteRequest request)
        {
            try
            {
                var note = await libraryService.SetItemNoteAsync(request.ItemType, request.SpotifyItemId, request.Content, request.Snapshot);
                return Ok(note);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("filters")] // POST api/library/filters
        public async Task<IActionResult> CreateSavedFilter([FromBody] SaveFilterModel model)
        {
            try
            {
                var id = await libraryService.CreateSavedFilterAsync(model.Scope, model.ContextId, model.Name, model.PayloadJson);
                return Ok(new { Id = id });
            }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPut("filters/{id}")] // rename or replace payload
        public async Task<IActionResult> UpdateSavedFilter(long id, [FromBody] SaveFilterModel model)
        {
            try
            {
                await libraryService.UpdateSavedFilterAsync(id, model.Name, model.PayloadJson);
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("filters/{id}")]
        public async Task<IActionResult> DeleteSavedFilter(long id)
        {
            try
            {
                await libraryService.DeleteSavedFilterAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet("playlists")]
        public async Task<IActionResult> GetUserPlaylists(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? search = null,
            [FromQuery] long? folderId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await liveLibraryService.GetPlaylistsAsync(sort, order, search, folderId, page, pageSize);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("playlists/{id}/tracks")]
        public async Task<IActionResult> GetPlaylistTracks(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await libraryService.GetPlaylistTracksAsync(id, page, pageSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("playlists/{id}/tags/counts")]
        public async Task<IActionResult> GetPlaylistTrackTagCounts(string id,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetPlaylistTrackTagCountsAsync(id, includeTags, includeMode, excludeTags, excludeMode, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("playlist-folders/{id}/parent")]
        public async Task<IActionResult> UpdateFolderParent(long id, [FromBody] FolderParentModel model)
        {
            try
            {
                await libraryService.UpdateFolderParentAsync(id, model.ParentId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("playlists/{id}/folder-assign")] // nowy endpoint assign (nie mylić ze starym)
        public async Task<IActionResult> AssignPlaylistFolder(string id, [FromBody] PlaylistFolderAssignModel model)
        {
            try
            {
                await libraryService.AssignPlaylistToFolderAsync(id, model.FolderId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public sealed class FolderParentModel
        { public long? ParentId { get; set; } }

        public sealed class PlaylistFolderAssignModel
        { public long? FolderId { get; set; } }

        [HttpGet("artists")]
        public async Task<IActionResult> GetUserArtists(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? includeTagStrengths = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? excludeTagStrengths = null,
            [FromQuery] string? search = null,
            [FromQuery] bool includeExternal = false)
        {
            try
            {
                var result = await liveLibraryService.GetArtistsAsync(sort, order, includeTags ?? tags, includeMode, excludeTags, excludeMode, genre, search, sortTagId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("artists/{id}")]
        public async Task<IActionResult> RemoveArtist(string id)
        {
            try
            {
                var result = await libraryService.UnfollowArtistLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { removed = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("artists/{id}")]
        public async Task<IActionResult> AddArtist(string id)
        {
            try
            {
                var result = await libraryService.FollowArtistLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { added = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public sealed class ArtistDiscoveryRequest
        {
            public List<string> ArtistIds { get; set; } = new();
        }

        [HttpPost("artists/discovery-add")]
        public async Task<IActionResult> AddArtistsForAlbumDiscovery([FromBody] ArtistDiscoveryRequest request)
        {
            try
            {
                return Ok(await libraryService.AddArtistsForAlbumDiscoveryAsync(request.ArtistIds));
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("albums/{id}")]
        public async Task<IActionResult> RemoveAlbum(string id)
        {
            try
            {
                var result = await libraryService.RemoveSavedAlbumLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { removed = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("albums/{id}")]
        public async Task<IActionResult> AddAlbum(string id)
        {
            try
            {
                var result = await libraryService.SaveAlbumLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { added = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public sealed class ItemStatusesRequest
        {
            public string ItemType { get; set; } = string.Empty;
            public List<string> SpotifyItemIds { get; set; } = new();
        }

        [HttpPost("items/status")]
        public async Task<IActionResult> GetItemStatuses([FromBody] ItemStatusesRequest request)
        {
            try
            {
                var result = await libraryService.GetLibraryItemStatusesAsync(request.ItemType, request.SpotifyItemIds);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public sealed class BulkAlbumRequest
        {
            public List<string> AlbumIds { get; set; } = new();
        }

        [HttpPost("albums/bulk")]
        public async Task<IActionResult> AddAlbumsBulk([FromBody] BulkAlbumRequest request)
        {
            var result = new List<object>();
            foreach (var albumId in request.AlbumIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
            {
                try
                {
                    var added = await libraryService.SaveAlbumLiveAsync(albumId);
                    if (added) await InvalidateLiveLibraryAsync();
                    result.Add(new { albumId, added, success = added });
                }
                catch (Exception ex)
                {
                    result.Add(new { albumId, added = false, success = false, error = ex.Message });
                }
            }
            return Ok(result);
        }

        [HttpDelete("tracks/{id}")]
        public async Task<IActionResult> RemoveTrack(string id)
        {
            try
            {
                var result = await libraryService.RemoveSavedTrackLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { removed = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("tracks/{id}")]
        public async Task<IActionResult> AddTrack(string id)
        {
            try
            {
                var result = await libraryService.SaveTrackLiveAsync(id);
                if (result) await InvalidateLiveLibraryAsync();
                return Ok(new { added = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tracks/{id}/exists")]
        public async Task<IActionResult> TrackExists(string id)
        {
            try
            {
                var result = await libraryService.IsTrackInLibraryAsync(id);
                return Ok(new { exists = result });
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("artists/tags")]
        public async Task<IActionResult> GetUserArtistTags()
        {
            try
            {
                var result = await libraryService.GetUserArtistTagsAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("artists/tags/counts")]
        public async Task<IActionResult> GetArtistTagCounts(
            [FromQuery] string? tags = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetArtistTagCountsAsync(tags, genre, includeTags, includeMode, excludeTags, excludeMode, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("artists/genres")]
        public async Task<IActionResult> GetArtistsGenres()
        {
            try
            {
                var result = await libraryService.GetArtistsLibraryGenres();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tracks")]
        public async Task<IActionResult> GetUserTracks(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? albumId = null,
            [FromQuery] int? year = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? includeTagStrengths = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? excludeTagStrengths = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await liveLibraryService.GetTracksAsync(sort, order, includeTags ?? tags, includeMode, excludeTags, excludeMode, search, page, pageSize);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tracks/tags/counts")]
        public async Task<IActionResult> GetTrackTagCounts(
            [FromQuery] string? tags = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? albumId = null,
            [FromQuery] int? year = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetTrackTagCountsAsync(tags, artistId, albumId, year, includeTags, includeMode, excludeTags, excludeMode, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("tracks/{id}")]
        public async Task<IActionResult> GetSingleTrack(string id)
        {
            try
            {
                var result = await libraryService.GetSingleTrackAsync(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("albums")]
        public async Task<IActionResult> GetUserAlbums(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? includeArtists = null,
            [FromQuery] string? excludeArtists = null,
            [FromQuery] int? fromYear = null,
            [FromQuery] int? toYear = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? includeTagStrengths = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? excludeTagStrengths = null,
            [FromQuery] string? includeGenres = null,
            [FromQuery] string? excludeGenres = null,
            [FromQuery] bool includeArtistTags = false,
            [FromQuery] string? search = null,
            [FromQuery] bool random = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await liveLibraryService.GetAlbumsAsync(sort, order, includeTags ?? tags, includeMode, excludeTags, excludeMode, search, includeArtistTags, page, pageSize);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("albums/tags")]
        public async Task<IActionResult> GetUserAlbumTags()
        {
            try
            {
                var result = await libraryService.GetUserAlbumTagsAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("albums/tags/counts")]
        public async Task<IActionResult> GetAlbumTagCounts(
            [FromQuery] string? tags = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? includeArtists = null,
            [FromQuery] string? excludeArtists = null,
            [FromQuery] int? fromYear = null,
            [FromQuery] int? toYear = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? includeGenres = null,
            [FromQuery] string? excludeGenres = null,
            [FromQuery] bool includeArtistTags = false,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetAlbumTagCountsAsync(tags, artistId, includeArtists, excludeArtists, fromYear, toYear, includeTags, includeMode, excludeTags, excludeMode, includeGenres, excludeGenres, includeArtistTags, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("albums/artists")]
        public async Task<IActionResult> GetAlbumsArtists(
            [FromQuery] string? tags = null,
            [FromQuery] int? fromYear = null,
            [FromQuery] int? toYear = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] string? includeArtists = null,
            [FromQuery] string? excludeArtists = null,
            [FromQuery] string? includeGenres = null,
            [FromQuery] string? excludeGenres = null,
            [FromQuery] bool includeArtistTags = false,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetAlbumsArtistsForUserAsync(tags, fromYear, toYear, includeTags, includeMode, excludeTags, excludeMode, includeArtists, excludeArtists, includeGenres, excludeGenres, includeArtistTags, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("albums/genres")]
        public async Task<IActionResult> GetAlbumGenres(
            [FromQuery] string? tags = null,
            [FromQuery] string? artistId = null,
            [FromQuery] string? includeArtists = null,
            [FromQuery] string? excludeArtists = null,
            [FromQuery] int? fromYear = null,
            [FromQuery] int? toYear = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] bool includeArtistTags = false,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetAlbumGenresForUserAsync(tags, artistId, includeArtists, excludeArtists, fromYear, toYear, includeTags, includeMode, excludeTags, excludeMode, includeArtistTags, search);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
