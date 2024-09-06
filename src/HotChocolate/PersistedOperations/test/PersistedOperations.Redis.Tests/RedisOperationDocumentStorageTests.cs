using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;

namespace HotChocolate.PersistedOperations.Redis;

public class RedisOperationDocumentStorageTests(RedisResource redisResource)
    : IClassFixture<RedisResource>
{
    private readonly IDatabase _database = redisResource.GetConnection().GetDatabase();

    [Fact]
    public Task Write_OperationDocument_To_Storage()
    {
        var snapshotName = Snapshot.FullName();
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

        return TryTest(async () =>
            {
                // arrange
                var storage = new RedisOperationDocumentStorage(_database);
                var document = new OperationDocumentSourceText("{ foo }");

                // act
                await storage.SaveAsync(documentId, document);

                // assert
                var buffer = ((byte[])await _database.StringGetAsync(documentId.Value))!;
                Utf8GraphQLParser.Parse(buffer).Print().MatchSnapshot(snapshotName);
            },
            () => _database.KeyDeleteAsync(documentId.Value));
    }

    [Fact]
    public Task Write_OperationDocument_documentId_Invalid()
    {
        var documentId = new OperationDocumentId();

        return TryTest(async () =>
        {
            var storage = new RedisOperationDocumentStorage(_database);
            var document = new OperationDocumentSourceText("{ foo }");

            // act
            async Task Action() => await storage.SaveAsync(documentId, document);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        });
    }

    [Fact]
    public Task Write_OperationDocument_OperationDocument_Is_Null()
    {
        return TryTest(async () =>
        {
            var storage = new RedisOperationDocumentStorage(_database);
            var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

            // act
            async Task Action() => await storage.SaveAsync(documentId, null!);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        });
    }

    [Fact]
    public Task Read_OperationDocument_From_Storage()
    {
        var snapshotName = Snapshot.FullName();
        var documentId = new OperationDocumentId(Guid.NewGuid().ToString("N"));

        return TryTest(async () =>
            {
                // arrange
                var storage = new RedisOperationDocumentStorage(_database);
                var buffer = "{ foo }"u8.ToArray();
                await _database.StringSetAsync(documentId.Value, buffer);

                // act
                var document = await storage.TryReadAsync(documentId);

                // assert
                Assert.NotNull(document);
                Assert.IsType<OperationDocument>(document).Document.Print().MatchSnapshot(snapshotName);
            },
            () => _database.KeyDeleteAsync(documentId.Value));
    }

    [Fact]
    public Task Read_OperationDocument_documentId_Invalid()
    {
        var documentId = new OperationDocumentId();
        return TryTest(async () =>
        {
            var storage = new RedisOperationDocumentStorage(_database);

            // act
            async Task Action() => await storage.TryReadAsync(documentId);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        });
    }

    private static async Task TryTest(
        Func<Task> action,
        Func<Task>? cleanup = null)
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
