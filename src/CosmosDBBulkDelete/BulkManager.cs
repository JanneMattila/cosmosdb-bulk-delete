using CosmosDBBulkDelete.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            await GenerateDataAsync();
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

        private async Task GenerateDataAsync()
        {
            var collection = Enumerable.Range(1, 1000);
            var tasks = new List<Task>();
            foreach (var item in collection)
            {
                var id = item.ToString();
                var task = _devicesContainer.CreateItemAsync(new Device()
                {
                    ID = id,
                    Name = $"Device {item}",
                    Current = new Location()
                    {
                        Latitude = 55,
                        Longitude = 44,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                }, new PartitionKey(id));

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            await Task.CompletedTask;
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
