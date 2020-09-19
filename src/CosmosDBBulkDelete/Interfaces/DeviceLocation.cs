using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class DeviceLocation
    {
        [JsonProperty(PropertyName = "id")]
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "deviceId")]
        [JsonPropertyName("deviceId")]
        public string DeviceID { get; set; }

        [JsonProperty(PropertyName = "location")]
        [JsonPropertyName("location")]
        public Location Location { get; set; }
    }
}
