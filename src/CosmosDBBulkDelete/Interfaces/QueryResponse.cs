using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CosmosDBBulkDelete.Interfaces
{
    public class QueryResponse
    {
        [JsonPropertyName("Documents")]
        public List<QueryResponseID> Documents { get; set; }
    }
}
