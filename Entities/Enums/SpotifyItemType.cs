namespace SpotifyBasicApi2025.Entities.Enums
{
    public static class SpotifyItemType
    {
        public const string Artist = "artist";
        public const string Album = "album";
        public const string Track = "track";
        public const string Playlist = "playlist";

        public static bool IsValid(string itemType)
        {
            return itemType switch
            {
                Artist => true,
                Album => true,
                Track => true,
                Playlist => true,
                _ => false
            };
        }

        public static string[] GetAllTypes()
        {
            return new[] { Artist, Album, Track, Playlist };
        }
    }
}
