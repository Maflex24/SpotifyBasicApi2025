namespace SpotifyBasicApi2025.Models
{
    public sealed class TagAssignmentStateModel
    {
        public long TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string TagGroupName { get; set; } = string.Empty;
        public float Strength { get; set; }
    }
}
