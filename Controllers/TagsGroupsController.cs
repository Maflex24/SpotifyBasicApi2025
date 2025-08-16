using Microsoft.AspNetCore.Mvc;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Enums;
using SpotifyBasicApi2025.Models;
using SpotifyBasicApi2025.Models.Extensions;
using SpotifyBasicApi2025.Services;

namespace SpotifyBasicApi2025.Controllers
{
    [ApiController]
    [Route("api/tags-groups")]
    public class TagsGroupsController : ControllerBase
    {
        private readonly TagsService tagsService;
        private readonly SpotifyDataService spotifyDataService;
        private readonly UserService userService;

        public TagsGroupsController(TagsService tagsService, SpotifyDataService spotifyDataService, UserService userService)
        {
            this.tagsService = tagsService;
            this.spotifyDataService = spotifyDataService;
            this.userService = userService;
        }

        // TODO: Pozostałe endpointy

        [HttpPost]
        public async Task<ActionResult<LibTagGroup>> CreateTagGroup(
            [FromBody] TagGroupModel model)
        {
            var userId = await userService.GetCurrentUserIdAsync();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Użytkownik nie jest zalogowany");

            var outModel = await tagsService.CreateTagGroupAsync(model, userId);

            return StatusCode(StatusCodes.Status202Accepted, outModel);
        }
    }
}