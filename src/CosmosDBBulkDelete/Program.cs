using Microsoft.Extensions.Configuration;
using System;

namespace CosmosDBBulkDelete
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cosmos DB Bulk Delete");
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            
            var connectionString = configuration.GetValue<string>("ConnectionString");
        }
    }
}
