using Newtonsoft.Json;

namespace CosmosDBBulkDelete.Interfaces
{
    public class Device
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "current")]
        public Location Current { get; set; }
    }
}
