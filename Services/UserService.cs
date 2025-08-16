using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Entities.Helpers;
using SpotifyBasicApi2025.Models;
using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Services
{
    public class UserService
    {
        private readonly DatabaseContext _context;
        private readonly SpotifyApiService _spotifyApiService;
        private readonly TokenManagerService _tokenManager;

        public UserService(DatabaseContext context, SpotifyApiService spotifyApiService, TokenManagerService tokenManager)
        {
            _context = context;
            _spotifyApiService = spotifyApiService;
            _tokenManager = tokenManager;
        }

        /// <summary>
        /// Pobiera aktualnie zalogowanego użytkownika Spotify
        /// </summary>
        /// <returns>ID użytkownika Spotify lub null jeśli nie jest zalogowany</returns>
        public async Task<string?> GetCurrentUserIdAsync()
        {
            // Sprawdź czy mamy ważny token
            if (!_tokenManager.HasValidToken())
            {
                return null;
            }

            try
            {
                // Pobierz profil użytkownika z Spotify API
                var userProfile = await _spotifyApiService.GetCurrentUserProfileAsync();
                
                if (userProfile == null)
                {
                    return null;
                }

                // Zapisz/aktualizuj użytkownika w bazie danych
                await SaveUserFromSpotifyProfileAsync(userProfile);

                return userProfile.Id;
            }
            catch (Exception ex)
            {
                // Loguj błąd, ale nie przerywaj działania
                Console.WriteLine($"Błąd podczas pobierania profilu użytkownika: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Pobiera pełny profil aktualnie zalogowanego użytkownika
        /// </summary>
        /// <returns>Profil użytkownika z bazy danych lub null jeśli nie jest zalogowany</returns>
        public async Task<SpUser?> GetCurrentUserAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await GetUserByIdAsync(userId);
        }

        /// <summary>
        /// Sprawdza czy użytkownik jest zalogowany
        /// </summary>
        /// <returns>True jeśli użytkownik jest zalogowany</returns>
        public async Task<bool> IsUserLoggedInAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return !string.IsNullOrEmpty(userId);
        }

        /// <summary>
        /// Pobiera użytkownika z bazy danych po ID
        /// </summary>
        /// <param name="userId">ID użytkownika Spotify</param>
        /// <returns>Użytkownik z bazy danych lub null</returns>
        public async Task<SpUser?> GetUserByIdAsync(string userId)
        {
            return await _context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// Pobiera wszystkich użytkowników z bazy danych
        /// </summary>
        /// <returns>Lista wszystkich użytkowników</returns>
        public async Task<List<SpUser>> GetAllUsersAsync()
        {
            return await _context.SpUsers
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        /// <summary>
        /// Zapisuje lub aktualizuje użytkownika w bazie danych na podstawie profilu Spotify
        /// </summary>
        /// <param name="spotifyProfile">Profil użytkownika z Spotify API</param>
        /// <returns>Zapisany/aktualizowany użytkownik</returns>
        private async Task<SpUser> SaveUserFromSpotifyProfileAsync(SpotifyUserProfile spotifyProfile)
        {
            // Sprawdź czy użytkownik już istnieje
            var existingUser = await _context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == spotifyProfile.Id);

            if (existingUser != null)
            {
                // Aktualizuj istniejącego użytkownika
                existingUser.DisplayName = spotifyProfile.DisplayName;
                existingUser.Type = spotifyProfile.Type;
                existingUser.Uri = spotifyProfile.Uri;
                existingUser.Href = spotifyProfile.Href;
                existingUser.Country = spotifyProfile.Country;
                existingUser.Email = spotifyProfile.Email;
                existingUser.Product = spotifyProfile.Product;
                existingUser.Followers = spotifyProfile.Followers.Total;
                existingUser.Images = StringListConverter.ListToString(spotifyProfile.Images.Select(img => img.Url).ToList());
                existingUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return existingUser;
            }
            else
            {
                // Dodaj nowego użytkownika
                var newUser = new SpUser
                {
                    Id = spotifyProfile.Id,
                    DisplayName = spotifyProfile.DisplayName,
                    Type = spotifyProfile.Type,
                    Uri = spotifyProfile.Uri,
                    Href = spotifyProfile.Href,
                    Country = spotifyProfile.Country,
                    Email = spotifyProfile.Email,
                    Product = spotifyProfile.Product,
                    Followers = spotifyProfile.Followers.Total,
                    Images = StringListConverter.ListToString(spotifyProfile.Images.Select(img => img.Url).ToList()),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SpUsers.Add(newUser);
                await _context.SaveChangesAsync();

                return newUser;
            }
        }

        /// <summary>
        /// Wymusza odświeżenie profilu użytkownika z Spotify API
        /// </summary>
        /// <returns>Zaktualizowany profil użytkownika</returns>
        public async Task<SpUser?> RefreshUserProfileAsync()
        {
            if (!_tokenManager.HasValidToken())
            {
                return null;
            }

            try
            {
                var userProfile = await _spotifyApiService.GetCurrentUserProfileAsync();
                
                if (userProfile == null)
                {
                    return null;
                }

                return await SaveUserFromSpotifyProfileAsync(userProfile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odświeżania profilu użytkownika: {ex.Message}");
                return null;
            }
        }
    }
}
