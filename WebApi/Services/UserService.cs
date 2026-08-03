using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Models;

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
        /// Pobiera ID użytkownika dla bieżącej sesji tokenu.
        /// Cache jest powiązany z wersją sesji, więc nie może przeżyć zmiany konta.
        /// </summary>
        public async Task<string?> GetCurrentUserIdAsync()
        {
            while (true)
            {
                var session = _tokenManager.GetUserSessionSnapshot();
                if (session == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(session.Value.CachedSpotifyUserId))
                    return session.Value.CachedSpotifyUserId;

                await _tokenManager.WaitForUserIdentityLoadAsync();
                try
                {
                    session = _tokenManager.GetUserSessionSnapshot();
                    if (session == null)
                        return null;

                    if (!string.IsNullOrWhiteSpace(session.Value.CachedSpotifyUserId))
                        return session.Value.CachedSpotifyUserId;

                    var userProfile = await _spotifyApiService.GetCurrentUserProfileAsync();
                    if (userProfile == null)
                        return null;

                    // The token may have changed while Spotify was answering.
                    // Discard that profile and retry against the active session.
                    if (!_tokenManager.IsSessionCurrent(session.Value.Version))
                        continue;

                    await _spotifyApiService.SaveUserFromSpotifyProfileAsync(userProfile);

                    if (_tokenManager.TryCacheSpotifyUserId(session.Value.Version, userProfile.Id))
                        return userProfile.Id;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas pobierania profilu użytkownika: {ex.Message}");
                    return null;
                }
                finally
                {
                    _tokenManager.ReleaseUserIdentityLoad();
                }
            }
        }

        /// <summary>
        /// Pobiera pełny profil aktualnie zalogowanego użytkownika z bazy danych.
        /// </summary>
        public async Task<SpUser?> GetCurrentUserAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return string.IsNullOrEmpty(userId) ? null : await GetUserByIdAsync(userId);
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            return !string.IsNullOrEmpty(userId);
        }

        public async Task<SpUser?> GetUserByIdAsync(string userId)
        {
            return await _context.SpUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<List<SpUser>> GetAllUsersAsync()
        {
            return await _context.SpUsers
                .OrderBy(u => u.DisplayName)
                .ToListAsync();
        }

        /// <summary>
        /// Wymusza pobranie profilu z Spotify i aktualizuje cache tylko wtedy,
        /// gdy sesja tokenu nie zmieniła się w trakcie żądania.
        /// </summary>
        public async Task<SpUser?> RefreshUserProfileAsync()
        {
            await _tokenManager.WaitForUserIdentityLoadAsync();
            try
            {
                var session = _tokenManager.GetUserSessionSnapshot();
                if (session == null)
                    return null;

                var userProfile = await _spotifyApiService.GetCurrentUserProfileAsync();
                if (userProfile == null || !_tokenManager.IsSessionCurrent(session.Value.Version))
                    return null;

                var user = await _spotifyApiService.SaveUserFromSpotifyProfileAsync(userProfile);
                return _tokenManager.TryCacheSpotifyUserId(session.Value.Version, userProfile.Id)
                    ? user
                    : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odświeżania profilu użytkownika: {ex.Message}");
                return null;
            }
            finally
            {
                _tokenManager.ReleaseUserIdentityLoad();
            }
        }
    }
}
