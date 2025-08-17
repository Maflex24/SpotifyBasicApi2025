using System.Text.Json;

namespace SpotifyBasicApi2025.Entities.Helpers
{
    public static class StringListConverter
    {
        private const char Separator = ',';

        public static string? ListToString(List<string>? list)
        {
            if (list == null || list.Count == 0)
                return null;

            return string.Join(Separator, list);
        }

        public static List<string> StringToList(string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new List<string>();

            return str.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => !string.IsNullOrEmpty(s))
                     .ToList();
        }

        public static string? JsonListToString(List<string>? list)
        {
            if (list == null || list.Count == 0)
                return null;

            return JsonSerializer.Serialize(list);
        }

        public static List<string> JsonStringToList(string? jsonStr)
        {
            if (string.IsNullOrWhiteSpace(jsonStr))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(jsonStr) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
