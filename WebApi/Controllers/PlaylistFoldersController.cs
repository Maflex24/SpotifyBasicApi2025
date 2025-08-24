using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/playlist-folders")]
    public class PlaylistFoldersController : ControllerBase
    {
        private readonly DatabaseContext dbContext;
        private readonly UserService userService;

        public PlaylistFoldersController(DatabaseContext dbContext, UserService userService)
        {
            this.dbContext = dbContext;
            this.userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return StatusCode(403);

            var folders = await dbContext.LibPlaylistFolders
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => new { f.Id, f.Name, f.ParentId })
                .ToListAsync();

            return Ok(folders);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FolderCreateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Name is required");

            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return StatusCode(403);

            var entity = new LibPlaylistFolder
            {
                Name = model.Name.Trim(),
                ParentId = model.ParentId,
                UserId = userId
            };

            if (entity.ParentId.HasValue)
            {
                var parentExists = await dbContext.LibPlaylistFolders.AnyAsync(f => f.Id == entity.ParentId.Value);

                if (!parentExists)
                    return BadRequest("Parent folder does not exist");
            }

            dbContext.LibPlaylistFolders.Add(entity);
            await dbContext.SaveChangesAsync();

            return Created($"api/playlist-folders/{entity.Id}", new { entity.Id, entity.Name, entity.ParentId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] FolderUpdateModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                return StatusCode(403);

            var entity = await dbContext.LibPlaylistFolders.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (entity == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(model.Name))
                entity.Name = model.Name.Trim();

            if (model.ParentId.HasValue || model.ParentId == null)
            {
                if (model.ParentId == entity.Id)
                    return BadRequest("Folder cannot be its own parent");

                if (model.ParentId.HasValue)
                {
                    var parentExists = await dbContext.LibPlaylistFolders.AnyAsync(f => f.Id == model.ParentId.Value);

                    if (!parentExists)
                        return BadRequest("Parent folder does not exist");
                }

                entity.ParentId = model.ParentId;
            }

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        public sealed class FolderCreateModel
        {
            public string Name { get; set; } = string.Empty;
            public long? ParentId { get; set; }
        }

        public sealed class FolderUpdateModel
        {
            public string? Name { get; set; }
            public long? ParentId { get; set; }
        }
    }
}
