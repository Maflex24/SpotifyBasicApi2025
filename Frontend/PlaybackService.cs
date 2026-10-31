using System.Net.Http.Json;
using System.Net;

namespace Frontend;

public sealed class PlaybackService(HttpClient http)
{
    public string? Error { get; private set; }
    public string? Warning { get; private set; }
    public event Action? Changed;

    public void ClearError()
    {
        Error = null;
        Warning = null;
        Changed?.Invoke();
    }

    public async Task<bool> PlayAsync(object body)
    {
        ClearError();
        try
        {
            using var response = await http.PutAsJsonAsync("api/player/play", body);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    var result = await response.Content.ReadFromJsonAsync<PlaybackResponse>();
                    Warning = result?.Warning;
                    Changed?.Invoke();
                }
                return true;
            }

            var detail = await response.Content.ReadAsStringAsync();
            Error = string.IsNullOrWhiteSpace(detail)
                ? $"Nie udało się rozpocząć odtwarzania (HTTP {(int)response.StatusCode})."
                : detail;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Error = "Nie udało się połączyć z odtwarzaczem. Spróbuj ponownie.";
        }
        Changed?.Invoke();
        return false;
    }

    private sealed class PlaybackResponse
    {
        public string? Warning { get; set; }
    }
}
