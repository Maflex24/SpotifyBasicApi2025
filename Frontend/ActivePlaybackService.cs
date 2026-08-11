using Frontend.Components;

namespace Frontend;

public sealed class ActivePlaybackService
{
    public string? CurrentAlbumId { get; private set; }

    public event Action? CurrentAlbumChanged;
    public event Action<string, IReadOnlyList<TagBadge>>? AlbumTagsChanged;
    public event Action<string, string?>? AlbumNoteChanged;

    public void SetCurrentAlbumId(string? albumId)
    {
        var normalizedId = string.IsNullOrWhiteSpace(albumId) ? null : albumId;
        if (string.Equals(CurrentAlbumId, normalizedId, StringComparison.Ordinal))
            return;

        CurrentAlbumId = normalizedId;
        CurrentAlbumChanged?.Invoke();
    }

    public void NotifyAlbumTagsChanged(string albumId, IReadOnlyList<TagBadge> tags)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
            AlbumTagsChanged?.Invoke(albumId, tags);
    }

    public void NotifyAlbumNoteChanged(string albumId, string? content)
    {
        if (!string.IsNullOrWhiteSpace(albumId))
            AlbumNoteChanged?.Invoke(albumId, string.IsNullOrWhiteSpace(content) ? null : content);
    }
}
