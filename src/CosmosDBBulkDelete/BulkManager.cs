using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CosmosDBBulkDelete
{
    public class BulkManager
    {
        private readonly CosmosClient _client;
        private Database _database;
        private Container _devicesContainer;
        private Container _deviceLocationsContainer;

        private const string DatabaseName = "db";

        private const string DevicesContainer = "devices";
        private const string DeviceLocationsContainer = "devicelocations";

        private const string BulkImport = "bulkImport";
        private const string BulkCleanUp = "bulkCleanUp";

        public BulkManager(string connectionString)
        {
            _client = new CosmosClient(connectionString, new CosmosClientOptions() { AllowBulkExecution = true });
        }

        public async Task ExecuteAsync()
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Console.WriteLine($"Creating database {DatabaseName}...");
            _database = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName, ThroughputProperties.CreateManualThroughput(400));

            Console.WriteLine($"Creating container {DevicesContainer}...");
            _devicesContainer = await _database.DefineContainer(DevicesContainer, "/id")
                    .WithDefaultTimeToLive(TimeSpan.FromDays(7))
                    .WithIndexingPolicy()
                        .WithIndexingMode(IndexingMode.Consistent)
                        .WithIncludedPaths()
                            .Attach()
                        .WithExcludedPaths()
                            .Path("/*")
                            .Attach()
                    .Attach()
                .CreateIfNotExistsAsync();

            Console.WriteLine($"Creating container {DeviceLocationsContainer}...");
            _deviceLocationsContainer = await _database.DefineContainer(DeviceLocationsContainer, "/deviceId")
                    .WithDefaultTimeToLive(TimeSpan.FromDays(7))
                    .WithIndexingPolicy()
                        .WithIndexingMode(IndexingMode.Consistent)
                        .WithIncludedPaths()
                            .Attach()
                        .WithExcludedPaths()
                            .Path("/*")
                            .Attach()
                    .Attach()
                .CreateIfNotExistsAsync();

            await CreateStoredProcedure(BulkImport);
            await CreateStoredProcedure(BulkCleanUp);
        }

        private async Task CreateStoredProcedure(string name)
        {
            Console.WriteLine($"Creating stored procedure {name}...");

            try
            {
                await _deviceLocationsContainer.Scripts.CreateStoredProcedureAsync(new StoredProcedureProperties()
                {
                    Id = name,
                    Body = File.ReadAllText($"{name}.js")
                });
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode != HttpStatusCode.Conflict)
                {
                    throw;
                }
            }
        }
    }
}
