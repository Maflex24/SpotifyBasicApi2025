using System.Text.Json;

namespace SpotifyBasicApi2025.Models.Extensions
{
    public static class ImageMappingExtensions
    {
        public static List<SpotifyImage> ParseSpotifyImages(string? images)
        {
            var result = new List<SpotifyImage>();

            if (string.IsNullOrWhiteSpace(images))
                return result;

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var imageArray = JsonSerializer.Deserialize<List<SpotifyImage>>(images, options);

                if (imageArray != null)
                    result.AddRange(imageArray);

                if (result.Count > 0)
                    return result;
            }
            catch (JsonException)
            {
            }

            var imageStrings = images.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i))
                .ToList();

            for (int i = 0; i < imageStrings.Count; i += 3)
            {
                if (i + 2 < imageStrings.Count)
                {
                    var urlStr = imageStrings[i];
                    var heightStr = imageStrings[i + 1];
                    var widthStr = imageStrings[i + 2];

                    var url = urlStr;

                    if (urlStr.Contains("\"url\":"))
                    {
                        var urlStart = urlStr.IndexOf("\"url\":\"") + 7;
                        var urlEnd = urlStr.IndexOf("\"", urlStart);

                        if (urlEnd > urlStart)
                            url = urlStr.Substring(urlStart, urlEnd - urlStart);
                    }

                    int? height = null;

                    if (heightStr.Contains("\"height\":"))
                    {
                        var heightStart = heightStr.IndexOf("\"height\":") + 9;
                        if (int.TryParse(heightStr.Substring(heightStart), out var h))
                            height = h;
                    }

                    int? width = null;

                    if (widthStr.Contains("\"width\":"))
                    {
                        var widthStart = widthStr.IndexOf("\"width\":") + 8;
                        var widthEnd = widthStr.IndexOf("}", widthStart);
                        var widthStrClean = widthEnd > widthStart
                            ? widthStr.Substring(widthStart, widthEnd - widthStart)
                            : widthStr.Substring(widthStart);

                        if (int.TryParse(widthStrClean, out var w))
                            width = w;
                    }

                    if (!string.IsNullOrEmpty(url))
                        result.Add(new SpotifyImage { Url = url, Height = height, Width = width });
                }
            }

            return result;
        }
    }
}


