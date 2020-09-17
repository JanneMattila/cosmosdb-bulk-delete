using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class Device
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("current")]
        public Location Current { get; set; }
    }
}
