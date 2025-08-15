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

        public async Task<SpotifyFollowedArtistsResponse?> GetFollowedArtists(int limit = 50)
        {
            var response = await requestService.MakeSpotifyRequestAsync($"me/following?type=artist&limit={limit}");
            
            if (response == null)
                return null;

            return JsonSerializer.Deserialize<SpotifyFollowedArtistsResponse>(response.ToString()!);
        }

        public async Task<List<SpotifyArtistObject>> GetAllFollowedArtists()
        {
            var allArtists = new List<SpotifyArtistObject>();
            string? after = null;
            const int limit = 50; // Maksymalny limit zgodnie z dokumentacją

            do
            {
                var endpoint = $"me/following?type=artist&limit={limit}";
                
                if (!string.IsNullOrEmpty(after))
                {
                    endpoint += $"&after={after}";
                }

                var response = await requestService.MakeSpotifyRequestAsync(endpoint);
                
                if (response == null)
                    break;

                var followedArtistsResponse = JsonSerializer.Deserialize<SpotifyFollowedArtistsResponse>(response.ToString()!);
                
                if (followedArtistsResponse?.Artists?.Items == null)
                    break;

                allArtists.AddRange(followedArtistsResponse.Artists.Items);
                
                // Pobierz cursor "after" dla następnej strony
                after = followedArtistsResponse.Artists.Cursors?.After;
                
                // Jeśli nie ma więcej stron, zakończ
                if (string.IsNullOrEmpty(after))
                    break;

            } while (true);

            return allArtists;
        }
    }
}