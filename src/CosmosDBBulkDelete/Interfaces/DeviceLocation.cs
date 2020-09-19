using Newtonsoft.Json;

namespace CosmosDBBulkDelete.Interfaces
{
    public class DeviceLocation
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceID { get; set; }

        [JsonProperty(PropertyName = "location")]
        public Location Location { get; set; }
    }
}
