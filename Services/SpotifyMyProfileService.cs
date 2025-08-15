namespace SpotifyBasicApi2025.Services
{
    public class SpotifyMyProfileService
    {
        private readonly SpotifyRequestService requestService;

        public SpotifyMyProfileService(SpotifyRequestService requestService)
        {
            this.requestService = requestService;
        }

        public async Task<object?> GetCurrentUserProfileAsync()
        {
            return await requestService.MakeSpotifyRequestAsync("me");
        }

        public async Task<object?> GetFollowedArtists(int limit = 10)
        {
            return await requestService.MakeSpotifyRequestAsync($"me/following?type=artist&limit={limit}");
        }

        public async Task<object?> GetUserSavedTracks(int limit = 10)
        {
            return await requestService.MakeSpotifyRequestAsync($"me/tracks?limit={limit}");
        }

        public async Task<object?> GetUserSavedAlbums(int limit = 10)
        {
            return await requestService.MakeSpotifyRequestAsync($"me/albums?limit={limit}");
        }

        public async Task<object?> GetCurrentUserTopItems(string type = "tracks", int limit = 10)
        {
            return await requestService.MakeSpotifyRequestAsync($"me/top/{type}?limit={limit}");
        }
    }
}