using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models.Pagination
{
	public class PagedRequest
	{
		[JsonPropertyName("page")]
		public int Page { get; set; } = 1;

		[JsonPropertyName("pageSize")]
		public int PageSize { get; set; } = 50;

		[JsonPropertyName("search")]
		public string? Search { get; set; }

		[JsonPropertyName("sort")]
		public string? Sort { get; set; }

		[JsonPropertyName("order")]
		public string? Order { get; set; }
	}
}

