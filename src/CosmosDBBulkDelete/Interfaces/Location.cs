using Newtonsoft.Json;
using System;

namespace CosmosDBBulkDelete.Interfaces
{
    public class Location
    {
        [JsonProperty(PropertyName = "lat")]
        public double Latitude { get; set; }

        [JsonProperty(PropertyName = "lon")]
        public double Longitude { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}
