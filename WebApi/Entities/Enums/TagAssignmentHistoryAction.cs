namespace SpotifyBasicApi2025.Entities.Enums
{
    public static class TagAssignmentHistoryAction
    {
        public const string Added = "added";
        public const string Removed = "removed";
        public const string StrengthChanged = "strengthChanged";

        public static bool IsValid(string action) => action is Added or Removed or StrengthChanged;
    }
}
