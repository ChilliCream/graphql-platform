using System;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using Xunit;

namespace HotChocolate.PersistedQueries.Redis
{
    public class RedisQueryStorageTests
        : IClassFixture<RedisResource>
    {
        private readonly IDatabase _database;

        public RedisQueryStorageTests(RedisResource redisResource)
        {
            _database = redisResource.GetConnection().GetDatabase();
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
                Utf8GraphQLParser.Parse(buffer).Print().MatchSnapshot(snapshotName);
            },
            () => _database.KeyDeleteAsync(queryId));
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public Task Write_Query_QueryId_Invalid(string queryId)
        {
            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);
                var query = new QuerySourceText("{ foo }");

                // act
                Task Action() => storage.WriteQueryAsync(queryId, query);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(Action);
            });
        }

        [Fact]
        public Task Write_Query_Query_Is_Null()
        {
            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);
                var queryId = Guid.NewGuid().ToString("N");

                // act
                Task Action() => storage.WriteQueryAsync(queryId, null!);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(Action);
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
                query.Document.Print().MatchSnapshot(snapshotName);
            },
            () => _database.KeyDeleteAsync(queryId));
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public Task Read_Query_QueryId_Invalid(string queryId)
        {
            return TryTest(async () =>
            {
                var storage = new RedisQueryStorage(_database);

                // act
                Task Action() => storage.TryReadQueryAsync(queryId);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(Action);
            });
        }

        private static async Task TryTest(
            Func<Task> action,
            Func<Task> cleanup = null)
        {
            // we will try four times ....
            var count = 0;
            var wait = 50;

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
                wait *= 2;
                count++;
            }
        }
    }
}
