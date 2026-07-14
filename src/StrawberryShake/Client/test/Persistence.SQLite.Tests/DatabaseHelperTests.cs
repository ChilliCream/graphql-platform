using Microsoft.Data.Sqlite;

namespace StrawberryShake.Persistence.SQLite;

public class DatabaseHelperTests
{
    [Fact]
    public async Task CreateDatabase()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        // act
        await helper.CreateIfNotExistsAsync(connection);

        // assert
        Assert.True(await helper.SaveEntityAsync(
            connection,
            new EntityDto
            {
                Id = "abc",
                Type = "def",
                Value = "ghi"
            },
            TestContext.Current.CancellationToken));

        Assert.True(await helper.SaveOperationAsync(
            connection,
            new OperationDto
            {
                Id = "abc",
                Variables = "def",
                ResultType = "ghi",
                DataInfo = "jkl"
            },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Entity_Create()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        // act
        Assert.True(await helper.SaveEntityAsync(
            connection,
            new EntityDto
            {
                Id = "abc",
                Type = "def",
                Value = "ghi"
            },
            TestContext.Current.CancellationToken));

        // assert
        var entities = new List<EntityDto>();
        await foreach (var entityDto in helper.GetAllEntitiesAsync(connection, TestContext.Current.CancellationToken))
        {
            entities.Add(entityDto);
        }

        Assert.Collection(
            entities,
            entity =>
            {
                Assert.Equal("abc", entity.Id);
                Assert.Equal("def", entity.Type);
                Assert.Equal("ghi", entity.Value);
            });
    }

    [Fact]
    public async Task Entity_Update()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        Assert.True(await helper.SaveEntityAsync(
            connection,
            new EntityDto
            {
                Id = "abc",
                Type = "def",
                Value = "ghi"
            },
            TestContext.Current.CancellationToken));

        // act
        Assert.True(await helper.SaveEntityAsync(
            connection,
            new EntityDto
            {
                Id = "abc",
                Type = "def1",
                Value = "ghi1"
            },
            TestContext.Current.CancellationToken));

        // assert
        var entities = new List<EntityDto>();
        await foreach (var entityDto in helper.GetAllEntitiesAsync(connection, TestContext.Current.CancellationToken))
        {
            entities.Add(entityDto);
        }

        Assert.Collection(
            entities,
            entity =>
            {
                Assert.Equal("abc", entity.Id);
                Assert.Equal("def1", entity.Type);
                Assert.Equal("ghi1", entity.Value);
            });
    }

    [Fact]
    public async Task Entity_Delete()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        Assert.True(await helper.SaveEntityAsync(
            connection,
            new EntityDto
            {
                Id = "abc",
                Type = "def",
                Value = "ghi"
            },
            TestContext.Current.CancellationToken));

        // act
        Assert.True(await helper.DeleteEntityAsync(connection, "abc", TestContext.Current.CancellationToken));

        // assert
        var entities = new List<EntityDto>();
        await foreach (var entityDto in helper.GetAllEntitiesAsync(connection, TestContext.Current.CancellationToken))
        {
            entities.Add(entityDto);
        }

        Assert.Empty(entities);
    }

    [Fact]
    public async Task Operation_Create()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        // act
        Assert.True(await helper.SaveOperationAsync(
            connection,
            new OperationDto
            {
                Id = "abc",
                Variables = "def",
                ResultType = "ghi",
                DataInfo = "jkl"
            },
            TestContext.Current.CancellationToken));

        // assert
        var collections = new List<OperationDto>();
        await foreach (var operationDto in helper.GetAllOperationsAsync(
            connection,
            TestContext.Current.CancellationToken))
        {
            collections.Add(operationDto);
        }

        Assert.Collection(
            collections,
            operation =>
            {
                Assert.Equal("abc", operation.Id);
                Assert.Equal("def", operation.Variables);
                Assert.Equal("ghi", operation.ResultType);
                Assert.Equal("jkl", operation.DataInfo);
            });
    }

    [Fact]
    public async Task Operation_Update()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        Assert.True(await helper.SaveOperationAsync(
            connection,
            new OperationDto
            {
                Id = "abc",
                Variables = "def",
                ResultType = "ghi",
                DataInfo = "jkl"
            },
            TestContext.Current.CancellationToken));

        // act
        Assert.True(await helper.SaveOperationAsync(
            connection,
            new OperationDto
            {
                Id = "abc",
                Variables = "def1",
                ResultType = "ghi1",
                DataInfo = "jkl1"
            },
            TestContext.Current.CancellationToken));

        // assert
        var collections = new List<OperationDto>();
        await foreach (var operationDto in helper.GetAllOperationsAsync(
            connection,
            TestContext.Current.CancellationToken))
        {
            collections.Add(operationDto);
        }

        Assert.Collection(
            collections,
            operation =>
            {
                Assert.Equal("abc", operation.Id);
                Assert.Equal("def1", operation.Variables);
                Assert.Equal("ghi1", operation.ResultType);
                Assert.Equal("jkl1", operation.DataInfo);
            });
    }

    [Fact]
    public async Task Operation_Delete()
    {
        // arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var helper = new DatabaseHelper();

        await helper.CreateIfNotExistsAsync(connection);

        Assert.True(await helper.SaveOperationAsync(
            connection,
            new OperationDto
            {
                Id = "abc",
                Variables = "def",
                ResultType = "ghi",
                DataInfo = "jkl"
            },
            TestContext.Current.CancellationToken));

        // act
        Assert.True(await helper.DeleteOperationAsync(connection, "abc", TestContext.Current.CancellationToken));

        // assert
        var collections = new List<OperationDto>();
        await foreach (var operationDto in helper.GetAllOperationsAsync(
            connection,
            TestContext.Current.CancellationToken))
        {
            collections.Add(operationDto);
        }

        Assert.Empty(collections);
    }
}
