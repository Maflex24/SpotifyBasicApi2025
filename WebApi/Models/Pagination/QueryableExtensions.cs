using Microsoft.EntityFrameworkCore;

namespace SpotifyBasicApi2025.Models.Pagination
{
	public static class QueryableExtensions
	{
		public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
		{
			if (page <= 0)
				page = 1;

			if (pageSize <= 0)
				pageSize = 50;

			var total = await query.CountAsync(cancellationToken);

			var items = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync(cancellationToken);

			return new PagedResult<T>
			{
				Items = items,
				Total = total,
				Page = page,
				PageSize = pageSize
			};
		}
	}
}

