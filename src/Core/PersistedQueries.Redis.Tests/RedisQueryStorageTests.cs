﻿using System;
using System.Threading.Tasks;
using System.Text;
using StackExchange.Redis;
using HotChocolate.PersistedQueries.FileSystem;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using Snapshooter;

namespace HotChocolate.PersistedQueries.Redis
{
    public class RedisQueryStorageTests
    {
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _database;

        public RedisQueryStorageTests()
        {
            string endpoint =
               Environment.GetEnvironmentVariable("REDIS_ENDPOINT")
               ?? "localhost:6379";

            string password =
                Environment.GetEnvironmentVariable("REDIS_PASSWORD");

            var configuration = new ConfigurationOptions
            {
                Ssl = !string.IsNullOrEmpty(password),
                AbortOnConnectFail = false,
                Password = password
            };

            configuration.EndPoints.Add(endpoint);

            _connectionMultiplexer =
                ConnectionMultiplexer.Connect(configuration);

            _database = _connectionMultiplexer.GetDatabase();
        }

        [Fact]
        public Task Write_Query_To_Storage()
        {
            SnapshotFullName snapshotName = Snapshot.FullName();
            var queryId = Guid.NewGuid().ToString("N");

            return TryTest(async () =>
            {
                // arrange
                var storage = new RedisQueryStorage(_database);
                var query = new QuerySourceText("{ foo }");

                // act
                await storage.WriteQueryAsync(queryId, query);

                // assert
                var buffer = (byte[])await _database.StringGetAsync(queryId);

                string s = QuerySyntaxSerializer.Serialize(
                    Utf8GraphQLParser.Parse(buffer));
                Snapshot.Match(s, snapshotName);
            },
            () => _database.KeyDeleteAsync(queryId));
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public Task Write_Query_QueryId_Invalid(string queryId)
        {
            SnapshotFullName snapshotName = Snapshot.FullName();

            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);
                var query = new QuerySourceText("{ foo }");

                // act
                Func<Task> action =
                    () => storage.WriteQueryAsync(queryId, query);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            });
        }

        [Fact]
        public Task Write_Query_Query_Is_Null()
        {
            SnapshotFullName snapshotName = Snapshot.FullName();

            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);
                var queryId = Guid.NewGuid().ToString("N");

                // act
                Func<Task> action =
                    () => storage.WriteQueryAsync(queryId, null);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            });
        }

        [Fact]
        public Task Read_Query_From_Storage()
        {
            SnapshotFullName snapshotName = Snapshot.FullName();
            var queryId = Guid.NewGuid().ToString("N");

            return TryTest(async () =>
            {
                // arrange
                var storage = new RedisQueryStorage(_database);
                var buffer = Encoding.UTF8.GetBytes("{ foo }");
                await _database.StringSetAsync(queryId, buffer);

                // act
                QueryDocument query = await storage.TryReadQueryAsync(queryId);

                // assert
                Assert.NotNull(query);
                string s = QuerySyntaxSerializer.Serialize(
                    query.Document);
                Snapshot.Match(s, snapshotName);
            },
            () => _database.KeyDeleteAsync(queryId));
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public Task Read_Query_QueryId_Invalid(string queryId)
        {
            SnapshotFullName snapshotName = Snapshot.FullName();

            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);

                // act
                Func<Task> action =
                    () => storage.TryReadQueryAsync(queryId);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            });
        }

        private static async Task TryTest(
            Func<Task> action,
            Func<Task> cleanup = null)
        {
            // we will try four times ....
            int count = 0;
            int wait = 50;

            while (true)
            {
                try
                {
                    if (count < 3)
                    {
                        try
                        {
                            await action().ConfigureAwait(false);
                            break;
                        }
                        catch
                        {
                            // try again
                        }
                    }
                    else
                    {
                        await action().ConfigureAwait(false);
                        break;
                    }
                }
                finally
                {
                    try
                    {
                        if (cleanup != null)
                        {
                            await cleanup().ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // ignore cleanup errors
                    }
                }

                await Task.Delay(wait).ConfigureAwait(false);
                wait = wait * 2;
                count++;
            }
        }
    }
}
