namespace Frontend.Components
{
    public sealed class TagBadge
    {
        public long TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string TagGroupName { get; set; } = string.Empty;
        public float Strength { get; set; }
    }
}


