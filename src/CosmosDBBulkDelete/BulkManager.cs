// Parts taken from
// https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/BulkExecutorMigration/Program.cs
using CosmosDBBulkDelete.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
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
            var random = new Random();
            var collection = Enumerable.Range(1, 1_000_000);
            var tasks = new List<Task>();

            var stopwatch = Stopwatch.StartNew();
            // Option 1:
            /*
            foreach (var item in collection)
            {
                var id = item.ToString();
                var partitionKey = new PartitionKey(id);
                var deviceTask = _devicesContainer.CreateItemAsync(new Device()
                {
                    ID = id,
                    Name = $"Device {item}",
                    Current = new Location()
                    {
                        Latitude = 55,
                        Longitude = 44,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                });

                tasks.Add(deviceTask);
                Console.Write("+");
            }
            */

            // Option 2:
            foreach (var item in collection)
            {
                var id = item.ToString();
                var partitionKey = new PartitionKey(id);
                var device = new Device()
                {
                    ID = id,
                    Name = $"Device {item}",
                    Current = new Location()
                    {
                        Latitude = 55,
                        Longitude = 44,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };
                var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, device);
                tasks.Add(_devicesContainer.CreateItemStreamAsync(stream, partitionKey));

                if (tasks.Count() > 1000)
                {
                    Console.Write("+");
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            Console.WriteLine("*");
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"Device import took: {stopwatch.Elapsed}.");

            stopwatch.Restart();
            foreach (var item in collection)
            {
                var id = item.ToString();
                var partitionKey = new PartitionKey(id);

                var deviceLocation = new DeviceLocation()
                {
                    DeviceID = id,
                    Location = new Location()
                    {
                        Latitude = 56,
                        Longitude = 45,
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };

                var deviceLocationTask = _deviceLocationsContainer.Scripts.ExecuteStoredProcedureAsync<string>(BulkImport, partitionKey, new dynamic[] { deviceLocation, random.Next(1200, 2500) });
                tasks.Add(deviceLocationTask);
                Console.Write("+");
            }

            stopwatch.Stop();
            Console.WriteLine($"Device location import took: {stopwatch.Elapsed}.");
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
