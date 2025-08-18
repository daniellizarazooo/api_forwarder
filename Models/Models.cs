using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Models
{
    public class Data
    {
        public string Token { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    // Define a class that matches the JSON structure
    public class LightingResponse
    {
        [JsonPropertyName("intensity")]
        public double Intensity { get; set; }
        public List<Link>? Links { get; set; }
    }

    public class Link
    {
        public string? Rel { get; set; }
        public string? Href { get; set; }
        public string? Method { get; set; }
    }

    public class SceneResponse
    {
        [JsonPropertyName("activeScene")]
        public byte ActiveScene { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool OutOfTune { get; set; }

        public List<Link>? Links { get; set; }

    }

    public class SceneToSet
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("scene")]
        public byte Scene { get; set; }
    }

    public class ErrorLogger
    {
        private readonly ILogger _logger;

        public ErrorLogger(ILogger<ErrorLogger> logger)
        {
            _logger = logger;
        }

        public void OnError(string message)
        {
            _logger.LogError(message, DateTime.UtcNow.ToLongTimeString());
        }

    }
}