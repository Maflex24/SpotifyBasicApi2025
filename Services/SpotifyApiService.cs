using System.Text.Json;
using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class SpotifyApiService
    {
        private readonly SpotifyRequestService requestService;

        public SpotifyApiService(SpotifyRequestService requestService)
        {
            this.requestService = requestService;
        }

        public async Task<object?> GetCurrentUserProfileAsync()
        {
            return await requestService.MakeSpotifyRequestAsync("me");
        }

        public async Task<object?> GetUserPlaylistsAsync()
        {
            return await requestService.MakeSpotifyRequestAsync("me/playlists");
        }

        public async Task<object?> SearchTracksAsync(string query, int limit = 20)
        {
            var encodedQuery = Uri.EscapeDataString(query);

            return await requestService.MakeSpotifyRequestAsync($"search?q={encodedQuery}&type=track&limit={limit}");
        }

        public async Task<object?> GetCurrentUserDevices()
        {
            return await requestService.MakeSpotifyRequestAsync("me/player/devices");
        }

        public async Task<object?> GetCurrentPlaybackState()
        {
            return await requestService.MakeSpotifyRequestAsync("me/player");
        }
    }
}