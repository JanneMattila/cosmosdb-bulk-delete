using Newtonsoft.Json;
using System;
using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class Location
    {
        [JsonProperty(PropertyName = "lat")]
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonProperty(PropertyName = "lon")]
        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}
