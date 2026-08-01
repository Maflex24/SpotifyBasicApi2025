namespace Frontend.Components
{
	public class TagItem
	{
		public long Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public long TagGroupId { get; set; }

		// Uzupełniane przez panel tagowania po pobraniu nazw grup.
		public string? TagGroupName { get; set; }
	}
}


