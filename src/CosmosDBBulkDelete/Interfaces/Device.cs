using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class Device
    {
        [JsonProperty(PropertyName = "id")]
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "current")]
        [JsonPropertyName("current")]
        public Location Current { get; set; }
    }
}
