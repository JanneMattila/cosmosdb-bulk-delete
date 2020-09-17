using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CosmosDBBulkDelete
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Cosmos DB Bulk Delete");
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            
            var connectionString = configuration.GetValue<string>("ConnectionString");

            var bulkManager = new BulkManager(connectionString);
            await bulkManager.ExecuteAsync();

            Console.WriteLine("Done!");
        }
    }
}
