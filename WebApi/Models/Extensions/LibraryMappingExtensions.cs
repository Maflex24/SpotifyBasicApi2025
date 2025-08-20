using SpotifyBasicApi2025.Entities;
using SpotifyBasicApi2025.Models;
using System.Text.Json;

namespace SpotifyBasicApi2025.Models.Extensions
{
    public static class LibraryMappingExtensions
    {
        #region TagGroups

        public static LibTagGroup MapToEntity(this TagGroupModel model)
        {
            return new LibTagGroup
            {
                Id = model.Id,
                Name = model.Name,
                ForAlbum = model.ForAlbum,
                ForArtist = model.ForArtist,
                ForPlaylist = model.ForPlaylist,
                ForTrack = model.ForTrack
            };
        }

        public static TagGroupModel MapToTagGroupModel(this LibTagGroup entity)
        {
            return new TagGroupModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ForAlbum = entity.ForAlbum,
                ForArtist = entity.ForArtist,
                ForPlaylist = entity.ForPlaylist,
                ForTrack = entity.ForTrack
            };
        }

        #endregion TagGroups

        #region Tags

        public static LibTag MapToEntity(this TagModel model)
        {
            return new LibTag
            {
                Id = model.Id,
                Name = model.Name,
                ForAlbum = model.ForAlbum,
                ForArtist = model.ForArtist,
                ForPlaylist = model.ForPlaylist,
                ForTrack = model.ForTrack,
                TagGroupId = model.TagGroupId
            };
        }

        public static TagModel MapToTagModel(this LibTag entity)
        {
            return new TagModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ForAlbum = entity.ForAlbum,
                ForArtist = entity.ForArtist,
                ForPlaylist = entity.ForPlaylist,
                ForTrack = entity.ForTrack,
                TagGroupId = entity.TagGroupId
            };
        }

        #endregion Tags

        #region Artists

        public static ArtistModel MapToArtistModel(this SpArtist entity, List<ArtistTagModel>? tags = null)
        {
            var images = new List<SpotifyImage>();
            
            if (!string.IsNullOrEmpty(entity.Images))
            {
                try
                {
                    // Próbujemy sparsować jako JSON array z ignorowaniem wielkości liter
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var imageArray = JsonSerializer.Deserialize<List<SpotifyImage>>(entity.Images, options);
                    if (imageArray != null)
                    {
                        images.AddRange(imageArray);
                    }
                }
                catch (JsonException)
                {
                    // Jeśli nie udało się sparsować jako JSON array, próbujemy jako string z separatorem
                    var imageStrings = entity.Images.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(i => i.Trim())
                        .Where(i => !string.IsNullOrEmpty(i))
                        .ToList();
                    
                    // Grupujemy stringi w pary (każdy obrazek ma kilka pól)
                    for (int i = 0; i < imageStrings.Count; i += 3)
                    {
                        if (i + 2 < imageStrings.Count)
                        {
                            var urlStr = imageStrings[i];
                            var heightStr = imageStrings[i + 1];
                            var widthStr = imageStrings[i + 2];
                            
                            // Wyciągamy URL z formatu "{\"url\":\"...\""
                            var url = urlStr;
                            if (urlStr.Contains("\"url\":"))
                            {
                                var urlStart = urlStr.IndexOf("\"url\":\"") + 7;
                                var urlEnd = urlStr.IndexOf("\"", urlStart);
                                if (urlEnd > urlStart)
                                {
                                    url = urlStr.Substring(urlStart, urlEnd - urlStart);
                                }
                            }
                            
                            // Wyciągamy height z formatu "\"height\":640"
                            int? height = null;
                            if (heightStr.Contains("\"height\":"))
                            {
                                var heightStart = heightStr.IndexOf("\"height\":") + 9;
                                if (int.TryParse(heightStr.Substring(heightStart), out var h))
                                {
                                    height = h;
                                }
                            }
                            
                            // Wyciągamy width z formatu "\"width\":640}"
                            int? width = null;
                            if (widthStr.Contains("\"width\":"))
                            {
                                var widthStart = widthStr.IndexOf("\"width\":") + 8;
                                var widthEnd = widthStr.IndexOf("}", widthStart);
                                var widthStrClean = widthEnd > widthStart 
                                    ? widthStr.Substring(widthStart, widthEnd - widthStart)
                                    : widthStr.Substring(widthStart);
                                if (int.TryParse(widthStrClean, out var w))
                                {
                                    width = w;
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(url))
                            {
                                images.Add(new SpotifyImage 
                                { 
                                    Url = url,
                                    Height = height,
                                    Width = width
                                });
                            }
                        }
                    }
                }
            }
            
            return new ArtistModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                Uri = entity.Uri,
                Href = entity.Href,
                Genres = !string.IsNullOrEmpty(entity.Genres)
                    ? entity.Genres.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(g => g.Trim()).ToList()
                    : new List<string>(),
                Popularity = entity.Popularity,
                Images = images,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Tags = tags ?? new List<ArtistTagModel>()
            };
        }

        public static ArtistTagModel MapToArtistTagModel(this LibTagAssign tagAssign, LibTag tag, LibTagGroup tagGroup)
        {
            return new ArtistTagModel
            {
                TagId = tagAssign.TagId,
                TagName = tag.Name,
                TagGroupName = tagGroup.Name,
                Strength = tagAssign.Strength
            };
        }

        #endregion Artists

		#region Albums

		public static AlbumModel MapToAlbumModel(this SpAlbum entity, List<AlbumTagModel>? tags = null)
		{
			var images = new List<SpotifyImage>();

			if (!string.IsNullOrEmpty(entity.Images))
			{
				try
				{
					var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var imageArray = JsonSerializer.Deserialize<List<SpotifyImage>>(entity.Images, options);

					if (imageArray != null)
						images.AddRange(imageArray);
				}
				catch (JsonException)
				{
					var imageStrings = entity.Images.Split(',', StringSplitOptions.RemoveEmptyEntries)
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
								images.Add(new SpotifyImage
								{
									Url = url,
									Height = height,
									Width = width
								});
						}
					}
				}
			}

			// Wyznacz rok wydania
			int? releaseYear = null;
			if (!string.IsNullOrWhiteSpace(entity.ReleaseDate))
			{
				if (DateTime.TryParse(entity.ReleaseDate, out var dt))
					releaseYear = dt.Year;
				else if (entity.ReleaseDate!.Length >= 4 && int.TryParse(entity.ReleaseDate.Substring(0, 4), out var y))
					releaseYear = y;
			}

			// Mapuj wykonawców z relacji SpAlbumArtistAssign
			var artists = new List<SimpleArtist>();
			if (entity.AlbumArtistAssigns != null && entity.AlbumArtistAssigns.Any())
			{
				foreach (var aa in entity.AlbumArtistAssigns)
				{
					artists.Add(new SimpleArtist { Id = aa.ArtistId, Name = aa.ArtistName });
				}
			}

			return new AlbumModel
			{
				Id = entity.Id,
				Name = entity.Name,
				Uri = entity.Uri,
				AlbumType = entity.AlbumType,
				TotalTracks = entity.TotalTracks,
				Popularity = entity.Popularity,
				ReleaseDate = entity.ReleaseDate,
				ReleaseYear = releaseYear,
				Images = images,
				CreatedAt = entity.CreatedAt,
				UpdatedAt = entity.UpdatedAt,
				Tags = tags ?? new List<AlbumTagModel>(),
				Artists = artists
			};
		}

		public static AlbumTagModel MapToAlbumTagModel(this LibTagAssign tagAssign, LibTag tag, LibTagGroup tagGroup)
		{
			return new AlbumTagModel
			{
				TagId = tagAssign.TagId,
				TagName = tag.Name,
				TagGroupName = tagGroup.Name,
				Strength = tagAssign.Strength
			};
		}

		#endregion Albums
    }
}