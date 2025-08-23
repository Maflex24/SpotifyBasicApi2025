using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/library")]
    public class LibraryController : ControllerBase
    {
        private readonly LibraryService libraryService;
        private readonly SpotifyApiService spotifyApiService;

        public LibraryController(LibraryService libraryService, SpotifyApiService spotifyApiService)
        {
            this.libraryService = libraryService;
            this.spotifyApiService = spotifyApiService;
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
                var result = await libraryService.GetUserPlaylistsAsync(sort, order, search, folderId, page, pageSize);
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

        public sealed class FolderParentModel { public long? ParentId { get; set; } }
        public sealed class PlaylistFolderAssignModel { public long? FolderId { get; set; } }

        [HttpGet("artists")] 
        public async Task<IActionResult> GetUserArtists(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? tags = null,
            [FromQuery] long? sortTagId = null,
            [FromQuery] string? genre = null,
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null)
        {
            try
            {
                var result = await libraryService.GetUserArtistsAsync(sort, order, tags, sortTagId, genre, includeTags, includeMode, excludeTags, excludeMode);

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

        [HttpDelete("artists/{id}")]
        public async Task<IActionResult> RemoveArtist(string id)
        {
            try
            {
                var result = await libraryService.RemoveArtistFromLibraryAsync(id);
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

        [HttpDelete("albums/{id}")]
        public async Task<IActionResult> RemoveAlbum(string id)
        {
            try
            {
                var result = await libraryService.RemoveAlbumFromLibraryAsync(id);
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

        [HttpDelete("tracks/{id}")]
        public async Task<IActionResult> RemoveTrack(string id)
        {
            try
            {
                var result = await libraryService.RemoveTrackFromLibraryAsync(id);
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
                var result = await libraryService.AddTrackToLibraryAsync(id);
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
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await libraryService.GetUserTracksAsync(
                    sort, order, tags, sortTagId, artistId, albumId, year,
                    includeTags, includeMode, excludeTags, excludeMode,
                    page, pageSize, search);
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
            [FromQuery] string? includeTags = null,
            [FromQuery] string? includeMode = null,
            [FromQuery] string? excludeTags = null,
            [FromQuery] string? excludeMode = null)
        {
            try
            {
                var result = await libraryService.GetUserAlbumsAsync(sort, order, tags, sortTagId, artistId, includeTags, includeMode, excludeTags, excludeMode);

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

        [HttpGet("albums/artists")]
        public async Task<IActionResult> GetAlbumsArtists()
        {
            try
            {
                var result = await libraryService.GetAlbumsArtistsForUserAsync();
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
    }
}


