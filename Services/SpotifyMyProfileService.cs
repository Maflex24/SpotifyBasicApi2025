using SpotifyBasicApi2025.Models;
using System.Text.Json;

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