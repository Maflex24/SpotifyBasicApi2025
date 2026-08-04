namespace Frontend.Components
{
	public class TagItem
	{
		public long Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public List<string> Aliases { get; set; } = new();

		public long TagGroupId { get; set; }

		public bool IsPinned { get; set; }

		public bool IsPinnedShortcut { get; set; }

		// Uzupełniane przez panel tagowania po pobraniu nazw grup.
		public string? TagGroupName { get; set; }
	}
}


