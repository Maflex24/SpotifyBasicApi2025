using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SpotifyBasicApi2025.Models
{
    public class ArtistModel
    {
        public string Id { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;
        
        public string? Type { get; set; }
        
        public string? Uri { get; set; }
        
        public string? Href { get; set; }
        
        public List<string> Genres { get; set; } = new List<string>();
        
        public int Popularity { get; set; }

        public bool IsInLibrary { get; set; }

        public float TagStrength { get; set; }
        
        public List<SpotifyImage> Images { get; set; } = new List<SpotifyImage>();
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        // Tagi przypisane do artysty z ich siłą
        public List<ArtistTagModel> Tags { get; set; } = new List<ArtistTagModel>();
    }
    
    public class ArtistTagModel
    {
        public long TagId { get; set; }
        
        public string TagName { get; set; } = string.Empty;
        
        public string TagGroupName { get; set; } = string.Empty;
        
        public float Strength { get; set; }
    }
    
    public class SpotifyImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }
        
        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }
}
