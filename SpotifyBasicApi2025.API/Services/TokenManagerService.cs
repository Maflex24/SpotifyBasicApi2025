using SpotifyBasicApi2025.Models;

namespace SpotifyBasicApi2025.Services
{
    public class TokenManagerService
    {
        private SpotifyTokenResponse? _currentToken;
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
                _currentToken = token;
            }
        }

        public void ClearToken()
        {
            lock (_lockObject)
            {
                _currentToken = null;
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
    }
}


