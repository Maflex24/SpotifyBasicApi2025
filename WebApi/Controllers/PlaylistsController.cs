using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/playlists")]
    public class PlaylistsController : ControllerBase
    {
        private readonly DatabaseContext dbContext;
        private readonly UserService userService;

        public PlaylistsController(DatabaseContext dbContext, UserService userService)
        {
            this.dbContext = dbContext;
            this.userService = userService;
        }

        [HttpPut("{playlistId}/folder")]
        public async Task<IActionResult> AssignFolder(string playlistId, [FromBody] AssignFolderModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return StatusCode(403);

            var playlist = await dbContext.SpPlaylists.FirstOrDefaultAsync(p => p.Id == playlistId && p.OwnerId == userId);

            if (playlist == null)
                return NotFound("Playlist not found");

            if (model.FolderId.HasValue)
            {
                var folderExists = await dbContext.LibPlaylistFolders.AnyAsync(f => f.Id == model.FolderId.Value);
                if (!folderExists)
                    return BadRequest("Folder not found");
            }

            playlist.FolderId = model.FolderId;
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        public sealed class AssignFolderModel
        {
            public long? FolderId { get; set; }
        }
    }
}


