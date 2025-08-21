namespace SpotifyBasicApi2025.Models
{
	public class TrackModel
	{
		public string Id { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public string? Uri { get; set; }

		public int DurationMs { get; set; }

		public int Popularity { get; set; }

		public string? AlbumId { get; set; }

		public string? AlbumName { get; set; }

		public int? ReleaseYear { get; set; }

		public List<SpotifyImage> Images { get; set; } = new List<SpotifyImage>();

		public List<TrackTagModel> Tags { get; set; } = new List<TrackTagModel>();

		public List<SimpleArtist> Artists { get; set; } = new List<SimpleArtist>();
	}

	public class TrackTagModel
	{
		public long TagId { get; set; }

		public string TagName { get; set; } = string.Empty;

		public string TagGroupName { get; set; } = string.Empty;

		public float Strength { get; set; }
	}
}



