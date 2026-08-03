using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class TokenManagerService
    {
        private SpotifyTokenResponse? _currentToken;
        private DateTimeOffset? _expiresAt;
        private long _sessionVersion;
        private string? _cachedSpotifyUserId;
        private readonly object _lockObject = new object();
        private readonly SemaphoreSlim _userIdentityLoadLock = new SemaphoreSlim(1, 1);

        public readonly record struct UserSessionSnapshot(long Version, string? CachedSpotifyUserId);

        public SpotifyTokenResponse? GetCurrentToken()
        {
            lock (_lockObject)
            {
                return _currentToken;
            }
        }

        public void SetToken(SpotifyTokenResponse token)
        {
            lock (_lockObject)
            {
                if (token.RefreshToken == null && _currentToken?.RefreshToken != null)
                    token.RefreshToken = _currentToken.RefreshToken;

                _currentToken = token;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
                _sessionVersion++;
                _cachedSpotifyUserId = null;
            }
        }

        public void ClearToken()
        {
            lock (_lockObject)
            {
                _currentToken = null;
                _expiresAt = null;
                _sessionVersion++;
                _cachedSpotifyUserId = null;
            }
        }

        public bool HasValidToken()
        {
            lock (_lockObject)
            {
                return _currentToken != null && !string.IsNullOrEmpty(_currentToken.AccessToken);
            }
        }

        public string? GetAccessToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.AccessToken;
            }
        }

        public string? GetRefreshToken()
        {
            lock (_lockObject)
            {
                return _currentToken?.RefreshToken;
            }
        }

        public bool IsExpiringSoon(int seconds = 60)
        {
            lock (_lockObject)
            {
                if (!_expiresAt.HasValue)
                    return false;
                return DateTimeOffset.UtcNow.AddSeconds(seconds) >= _expiresAt.Value;
            }
        }

        public DateTimeOffset? GetExpiresAt()
        {
            lock (_lockObject)
            {
                return _expiresAt;
            }
        }

        public UserSessionSnapshot? GetUserSessionSnapshot()
        {
            lock (_lockObject)
            {
                if (_currentToken == null || string.IsNullOrWhiteSpace(_currentToken.AccessToken))
                    return null;

                return new UserSessionSnapshot(_sessionVersion, _cachedSpotifyUserId);
            }
        }

        public bool IsSessionCurrent(long version)
        {
            lock (_lockObject)
            {
                return _currentToken != null
                    && !string.IsNullOrWhiteSpace(_currentToken.AccessToken)
                    && _sessionVersion == version;
            }
        }

        public bool TryCacheSpotifyUserId(long version, string spotifyUserId)
        {
            lock (_lockObject)
            {
                if (_currentToken == null
                    || string.IsNullOrWhiteSpace(_currentToken.AccessToken)
                    || _sessionVersion != version)
                    return false;

                _cachedSpotifyUserId = spotifyUserId;
                return true;
            }
        }

        public Task WaitForUserIdentityLoadAsync() => _userIdentityLoadLock.WaitAsync();

        public void ReleaseUserIdentityLoad() => _userIdentityLoadLock.Release();
    }
}
