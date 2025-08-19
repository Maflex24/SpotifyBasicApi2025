using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpotifyBasicApi2025.Entities
{
    public class LibTagAssign
    {
        [Key]
        public long Id { get; set; }

        public long TagId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Range(0.0, 6.0)]
        public float Strength { get; set; } = 3.0f;

        // Dyskryminator - określa typ elementu
        public string ItemType { get; set; } = string.Empty; // "artist", "album", "track", "playlist"

        // ID elementu Spotify (SpArtist.Id, SpAlbum.Id, SpTrack.Id, SpPlaylist.Id)
        public string SpotifyItemId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relacja do tagu
        [ForeignKey("TagId")]
        public virtual LibTag Tag { get; set; } = null!;

        // Relacja do użytkownika Spotify
        [ForeignKey("UserId")]
        public virtual SpUser User { get; set; } = null!;

        // Referencje do elementów Spotify przechowujemy jako miękkie powiązanie (ItemType + SpotifyItemId)
        // Bez bezpośrednich kluczy obcych do tabel SpArtist/SpAlbum/SpTrack/SpPlaylist
    }
}