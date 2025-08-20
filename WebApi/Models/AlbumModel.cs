namespace SpotifyBasicApi2025.Models
{
	public class AlbumModel
	{
		public string Id { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public string? Uri { get; set; }

		public string AlbumType { get; set; } = string.Empty;

		public int TotalTracks { get; set; }

		public int Popularity { get; set; }

		public List<SpotifyImage> Images { get; set; } = new List<SpotifyImage>();

		public DateTime CreatedAt { get; set; }

		public DateTime UpdatedAt { get; set; }

		public List<AlbumTagModel> Tags { get; set; } = new List<AlbumTagModel>();
	}

	public class AlbumTagModel
	{
		public long TagId { get; set; }

		public string TagName { get; set; } = string.Empty;

		public string TagGroupName { get; set; } = string.Empty;

		public float Strength { get; set; }
	}
}


