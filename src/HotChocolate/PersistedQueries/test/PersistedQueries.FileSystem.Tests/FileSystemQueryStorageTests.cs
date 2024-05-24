using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedQueries.FileSystem;

public class FileSystemQueryStorageTests
{
    [Fact]
    public async Task Write_Query_To_Storage()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemQueryStorage(new DefaultQueryFileMap(path));

            var query = new OperationDocumentSourceText("{ foo }");
            var documentId = new OperationDocumentId("1234");

            // act
            await storage.SaveAsync(documentId, query);

            // assert
            Assert.True(File.Exists(IOPath.Combine(path, "1234.graphql")));
            var content = await File.ReadAllBytesAsync(IOPath.Combine(path, "1234.graphql"));
            Utf8GraphQLParser.Parse(content).Print().MatchSnapshot();
        }
        finally
        {
            if (path != null)
            {
                Directory.Delete(path, true);
            }
        }
    }
    
    [Fact]
    public async Task Write_Query_documentId_Invalid()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemQueryStorage(new DefaultQueryFileMap(path));

            var query = new OperationDocumentSourceText("{ foo }");

            // act
            async Task Action() => await storage.SaveAsync(new OperationDocumentId(), query);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        }
        finally
        {
            if (path != null)
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Fact]
    public async Task Write_Query_Query_Is_Null()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemQueryStorage(new DefaultQueryFileMap(path));

            // act
            async Task Action() => await storage.SaveAsync(new OperationDocumentId("1234"), null!);

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        }
        finally
        {
            if (path != null)
            {
                Directory.Delete(path, true);
            }
        }
    }

    [Fact]
    public async Task Read_Query_From_Storage()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemQueryStorage(new DefaultQueryFileMap(path));

            var documentId = "1234";
            await File.WriteAllTextAsync(IOPath.Combine(path, documentId + ".graphql"), "{ foo }");

            // act
            var query = await storage.TryReadAsync(new OperationDocumentId(documentId));

            // assert
            Assert.NotNull(query);
            Assert.IsType<OperationDocument>(query).Document!.ToString().MatchSnapshot();
        }
        finally
        {
            if (path != null)
            {
                Directory.Delete(path, true);
            }
        }
    }
    
    [Fact]
    public async Task Read_Query_documentId_Invalid()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemQueryStorage(new DefaultQueryFileMap(path));

            // act
            async Task Action() => await storage.TryReadAsync(new OperationDocumentId());

            // assert
            await Assert.ThrowsAsync<ArgumentNullException>(Action);
        }
        finally
        {
            if (path != null)
            {
                Directory.Delete(path, true);
            }
        }
    }
}
