using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class QueryResponseID
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }
    }
}
