using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class DeviceLocation
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("deviceId")]
        public string DeviceID { get; set; }

        [JsonPropertyName("location")]
        public Location Location { get; set; }
    }
}
