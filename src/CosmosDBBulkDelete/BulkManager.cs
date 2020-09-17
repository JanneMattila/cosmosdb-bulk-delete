using Microsoft.Azure.Cosmos;
using System;
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
            _database = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName, ThroughputProperties.CreateManualThroughput(400));

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

            _deviceLocationsContainer = await _database.DefineContainer(DeviceLocationsContainer, "/id")
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
        }
    }
}
