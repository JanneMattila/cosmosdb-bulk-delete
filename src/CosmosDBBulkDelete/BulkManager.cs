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

        private const int _from = 1;
        private const int _to = 100_001;
        private const long _locationsPerDevice = 2_000;
        private const int _batch = 20_000;

        public BulkManager(string connectionString)
        {
            _client = new CosmosClient(connectionString, new CosmosClientOptions() { AllowBulkExecution = true });
        }

        public async Task ExecuteAsync()
        {
            var requestUnitsScenarios = new int[] { 25_000, 50_000, 100_000 };
            foreach (var requestUnits in requestUnitsScenarios)
            {
                Console.WriteLine($"Executing test with {requestUnits} RUs. Tests create and delete items from {_from} to {_to}:");
                await PreTestAsync(requestUnits);

                await GenerateDataAsync();
                await DeleteDataAsync();

                await PostTestAsync();
            }
        }

        private async Task PreTestAsync(int requestUnits)
        {
            Console.WriteLine($"Creating database {DatabaseName}...");
            _database = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName, ThroughputProperties.CreateManualThroughput(requestUnits));

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

        private async Task PostTestAsync()
        {
            await _database.DeleteAsync();
        }

        private async Task GenerateDataAsync()
        {
            var random = new Random();
            var collection = Enumerable.Range(_from, _to);
            var tasks = new List<Task>();

            var stopwatch = Stopwatch.StartNew();
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

                if (tasks.Count() > _batch)
                {
                    Console.Write("+");
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            Console.WriteLine("*");
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"Device import took: {stopwatch.Elapsed}. Documents/s: {(_to - _from) * 1000 / stopwatch.ElapsedMilliseconds}");

            stopwatch.Restart();
            foreach (var item in collection)
            {
                var id = item.ToString();
                var partitionKey = new PartitionKey(id);

                for (int i = 1; i < _locationsPerDevice + 1; i++)
                {
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
                    var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(stream, deviceLocation);
                    tasks.Add(_deviceLocationsContainer.CreateItemStreamAsync(stream, partitionKey));
                }

                if (tasks.Count() > _batch)
                {
                    Console.Write("+");
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            Console.WriteLine("*");
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"Device location import took: {stopwatch.Elapsed}. Documents/s: {(_to - _from) * _locationsPerDevice * 1000 / stopwatch.ElapsedMilliseconds}");

            //stopwatch.Restart();
            //foreach (var item in collection)
            //{
            //    var id = item.ToString();
            //    var partitionKey = new PartitionKey(id);

            //    var deviceLocation = new DeviceLocation()
            //    {
            //        DeviceID = id,
            //        Location = new Location()
            //        {
            //            Latitude = 56,
            //            Longitude = 45,
            //            Timestamp = DateTimeOffset.UtcNow
            //        }
            //    };

            //    await _deviceLocationsContainer.Scripts.ExecuteStoredProcedureAsync<string>(BulkImport, partitionKey, new dynamic[] { deviceLocation, random.Next(1200, 2500) });
            //    Console.Write("+");
            //}

            //stopwatch.Stop();
            //Console.WriteLine($"Device location import took: {stopwatch.Elapsed}.");
        }

        private async Task QueryDataAsync()
        {
            var collection = Enumerable.Range(_from, _to);
            var stopwatch = Stopwatch.StartNew();
            foreach (var deviceId in collection)
            {
                using var queryIterator = _deviceLocationsContainer.GetItemQueryStreamIterator("SELECT c.id FROM c", null,
                    new QueryRequestOptions()
                    {
                        PartitionKey = new PartitionKey(deviceId.ToString())
                    });
                while (queryIterator.HasMoreResults)
                {
                    using var response = await queryIterator.ReadNextAsync();
                    var queryResponse = await JsonSerializer.DeserializeAsync<QueryResponse>(response.Content);
                }
            }

            stopwatch.Stop();

            Console.WriteLine($"Device locations query took: {stopwatch.Elapsed}. Documents/s: {(_to - _from) * 1000 / stopwatch.ElapsedMilliseconds}.");
        }

        private async Task DeleteDataAsync()
        {
            var collection = Enumerable.Range(_from, _to);
            var tasks = new List<Task>();

            var stopwatch = Stopwatch.StartNew();
            foreach (var item in collection)
            {
                var id = item.ToString();
                var partitionKey = new PartitionKey(id);
                tasks.Add(_devicesContainer.DeleteItemStreamAsync(id, partitionKey));

                if (tasks.Count() > _batch)
                {
                    Console.Write("-");
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            Console.WriteLine("*");
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"Device delete took: {stopwatch.Elapsed}. Documents/s: {(_to - _from) * 1000 / stopwatch.ElapsedMilliseconds}.");

            //stopwatch.Restart();
            //foreach (var item in collection)
            //{
            //    var id = item.ToString();
            //    var partitionKey = new PartitionKey(id);

            //    while (true)
            //    {
            //        Console.Write("-");
            //        var result = await _deviceLocationsContainer.Scripts.ExecuteStoredProcedureAsync<BulkResponse>(BulkCleanUp, partitionKey, new dynamic[] { 1000 });
            //        if (!result.Resource.Continuation)
            //        {
            //            break;
            //        }
            //    }
            //}

            //stopwatch.Stop();
            //Console.WriteLine($"Device location delete took: {stopwatch.Elapsed}.");
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
