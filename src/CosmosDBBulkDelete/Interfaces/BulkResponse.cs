using Newtonsoft.Json;

namespace CosmosDBBulkDelete.Interfaces
{
    public class BulkResponse
    {
        [JsonProperty(PropertyName = "deleted")]
        public int Deleted { get; set; }

        [JsonProperty(PropertyName = "continuation")]
        public bool Continuation { get; set; }
    }
}
