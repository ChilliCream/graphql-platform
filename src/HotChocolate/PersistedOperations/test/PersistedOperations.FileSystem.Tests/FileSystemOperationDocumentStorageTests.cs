using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedOperations.FileSystem;

public class FileSystemOperationDocumentStorageTests
{
    [Fact]
    public async Task Write_OperationDocument_To_Storage()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemOperationDocumentStorage(new DefaultOperationDocumentFileMap(path));

            var document = new OperationDocumentSourceText("{ foo }");
            var documentId = new OperationDocumentId("1234");

            // act
            await storage.SaveAsync(documentId, document);

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
    public async Task Write_OperationDocument_documentId_Invalid()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemOperationDocumentStorage(new DefaultOperationDocumentFileMap(path));

            var document = new OperationDocumentSourceText("{ foo }");

            // act
            async Task Action() => await storage.SaveAsync(new OperationDocumentId(), document);

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
    public async Task Write_OperationDocument_OperationDocument_Is_Null()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemOperationDocumentStorage(new DefaultOperationDocumentFileMap(path));

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
    public async Task Read_OperationDocument_From_Storage()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemOperationDocumentStorage(new DefaultOperationDocumentFileMap(path));

            var documentId = "1234";
            await File.WriteAllTextAsync(IOPath.Combine(path, documentId + ".graphql"), "{ foo }");

            // act
            var document = await storage.TryReadAsync(new OperationDocumentId(documentId));

            // assert
            Assert.NotNull(document);
            Assert.IsType<OperationDocument>(document).Document!.ToString().MatchSnapshot();
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
    public async Task Read_OperationDocument_documentId_Invalid()
    {
        string? path = null;

        try
        {
            // arrange
            path = IOPath.Combine(IOPath.GetTempPath(), "d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            var storage = new FileSystemOperationDocumentStorage(new DefaultOperationDocumentFileMap(path));

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
