using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class TokenManagerService
    {
        private SpotifyTokenResponse? _currentToken;
        private DateTimeOffset? _expiresAt;
        private readonly object _lockObject = new object();

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
            }
        }

        public void ClearToken()
        {
            lock (_lockObject)
            {
                _currentToken = null;
                _expiresAt = null;
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
    }
}