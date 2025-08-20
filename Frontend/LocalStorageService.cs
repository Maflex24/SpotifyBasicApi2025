using System.Text.Json;
using Microsoft.JSInterop;

namespace Frontend
{
	public class LocalStorageService
	{
		private readonly IJSRuntime jsRuntime;

		public LocalStorageService(IJSRuntime jsRuntime)
		{
			this.jsRuntime = jsRuntime;
		}

		public async Task SetItemAsync<T>(string key, T value)
		{
			try
			{
				var json = JsonSerializer.Serialize(value);
				await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
			}
			catch
			{
				// ignoruj błędy dostępu do localStorage (np. przed pierwszym renderem)
			}
		}

		public async Task<T?> GetItemAsync<T>(string key)
		{
			try
			{
				var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
				if (string.IsNullOrWhiteSpace(json))
					return default;
				return JsonSerializer.Deserialize<T>(json);
			}
			catch
			{
				return default;
			}
		}

		public async Task RemoveItemAsync(string key)
		{
			await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
		}
	}
}


