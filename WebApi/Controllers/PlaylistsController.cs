using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/playlists")]
    public class PlaylistsController : ControllerBase
    {
        private readonly DatabaseContext dbContext;

        public PlaylistsController(DatabaseContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpPut("{playlistId}/folder")]
        public async Task<IActionResult> AssignFolder(string playlistId, [FromBody] AssignFolderModel model)
        {
            var playlist = await dbContext.SpPlaylists.FirstOrDefaultAsync(p => p.Id == playlistId);

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


