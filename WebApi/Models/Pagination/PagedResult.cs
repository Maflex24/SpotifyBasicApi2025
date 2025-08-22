using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models.Pagination
{
	public class PagedResult<T>
	{
		[JsonPropertyName("items")]
		public List<T> Items { get; set; } = new();

		[JsonPropertyName("total")]
		public int Total { get; set; }

		[JsonPropertyName("page")]
		public int Page { get; set; }

		[JsonPropertyName("pageSize")]
		public int PageSize { get; set; }

		[JsonPropertyName("totalPages")]
		public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
	}
}

