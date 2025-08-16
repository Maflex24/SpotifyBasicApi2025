using System.Text.Json;

namespace SpotifyBasicApi2025.Models.Extensions
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Konwertuje obiekt na string JSON z domyślnymi opcjami (wcięcia, camelCase)
        /// </summary>
        /// <param name="obj">Obiekt do konwersji</param>
        /// <returns>String w formacie JSON</returns>
        public static string ToJson(this object obj)
        {
            if (obj == null)
                return "null";

            return JsonSerializer.Serialize(obj, DefaultOptions);
        }

        /// <summary>
        /// Konwertuje obiekt na string JSON z niestandardowymi opcjami
        /// </summary>
        /// <param name="obj">Obiekt do konwersji</param>
        /// <param name="options">Opcje serializacji JSON</param>
        /// <returns>String w formacie JSON</returns>
        public static string ToJson(this object obj, JsonSerializerOptions options)
        {
            if (obj == null)
                return "null";

            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Konwertuje obiekt na string JSON bez wcięć (kompaktowy format)
        /// </summary>
        /// <param name="obj">Obiekt do konwersji</param>
        /// <returns>String w formacie JSON bez wcięć</returns>
        public static string ToJsonCompact(this object obj)
        {
            if (obj == null)
                return "null";

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Konwertuje obiekt na string JSON z oryginalnymi nazwami właściwości (bez camelCase)
        /// </summary>
        /// <param name="obj">Obiekt do konwersji</param>
        /// <returns>String w formacie JSON z oryginalnymi nazwami</returns>
        public static string ToJsonWithOriginalNames(this object obj)
        {
            if (obj == null)
                return "null";

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            };

            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Konwertuje listę obiektów na string JSON
        /// </summary>
        /// <typeparam name="T">Typ obiektów w liście</typeparam>
        /// <param name="list">Lista obiektów do konwersji</param>
        /// <returns>String w formacie JSON</returns>
        public static string ToJson<T>(this IEnumerable<T> list)
        {
            if (list == null)
                return "null";

            return JsonSerializer.Serialize(list, DefaultOptions);
        }

        /// <summary>
        /// Konwertuje listę obiektów na string JSON z niestandardowymi opcjami
        /// <summary>
        /// <typeparam name="T">Typ obiektów w liście</typeparam>
        /// <param name="list">Lista obiektów do konwersji</param>
        /// <param name="options">Opcje serializacji JSON</param>
        /// <returns>String w formacie JSON</returns>
        public static string ToJson<T>(this IEnumerable<T> list, JsonSerializerOptions options)
        {
            if (list == null)
                return "null";

            return JsonSerializer.Serialize(list, options);
        }
    }
}