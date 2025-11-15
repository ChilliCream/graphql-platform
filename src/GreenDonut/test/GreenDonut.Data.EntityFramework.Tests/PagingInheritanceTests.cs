using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingInheritanceTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task BatchPaging_With_TPC_Selector_And_Navigation_Property()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedFileSystemAsync(connectionString);

        await using var context = new FileSystemContext(connectionString);

        var query = new QueryContext<FileSystemEntry>(
            Selector: x => new FileSystemDirectory { Id = x.Id, Name = x.Name });

        var parentIds = new[] { 1, 2 };

        // act
        var result = await context
            .Entries
            .AsNoTracking()
            .Where(x => x.Parent != null && parentIds.Contains(x.Parent.Id))
            .With(query, x => x.AddAscending(y => y.Id))
            .ToBatchPageAsync(
                keySelector: x => x.Parent!.Id,
                arguments: new PagingArguments(10));

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task BatchPaging_With_TPC_Selector_And_Scalar_Property()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedFileSystemAsync(connectionString);

        await using var context = new FileSystemContext(connectionString);

        var query = new QueryContext<FileSystemEntry>(
            Selector: x => new FileSystemDirectory { Id = x.Id, Name = x.Name });

        var parentIds = new[] { 1, 2 };

        // act
        var result = await context
            .Entries
            .AsNoTracking()
            .Where(x => x.ParentId != null && parentIds.Contains(x.ParentId.Value))
            .With(query, x => x.AddAscending(y => y.Id))
            .ToBatchPageAsync(
                keySelector: x => x.ParentId!.Value,
                arguments: new PagingArguments(10));

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
    }

    private static async Task SeedFileSystemAsync(string connectionString)
    {
        await using var context = new FileSystemContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        // create root directories
        var root = new FileSystemDirectory { Id = 1, Name = "root" };
        var documents = new FileSystemDirectory { Id = 2, Name = "documents", ParentId = 1 };
        var pictures = new FileSystemDirectory { Id = 3, Name = "pictures", ParentId = 1 };

        context.Directories.AddRange(root, documents, pictures);

        // create subdirectories
        var work = new FileSystemDirectory { Id = 4, Name = "work", ParentId = 2 };
        var personal = new FileSystemDirectory { Id = 5, Name = "personal", ParentId = 2 };
        var photos = new FileSystemDirectory { Id = 6, Name = "photos", ParentId = 3 };

        context.Directories.AddRange(work, personal, photos);

        // create files
        var file1 = new FileSystemFile { Id = 7, Name = "report.pdf", ParentId = 4, Size = 1024, Extension = "pdf" };
        var file2 = new FileSystemFile { Id = 8, Name = "notes.txt", ParentId = 4, Size = 512, Extension = "txt" };
        var file3 = new FileSystemFile { Id = 9, Name = "resume.docx", ParentId = 5, Size = 2048, Extension = "docx" };
        var file4 = new FileSystemFile { Id = 10, Name = "vacation.jpg", ParentId = 6, Size = 4096, Extension = "jpg" };
        var file5 = new FileSystemFile { Id = 11, Name = "family.jpg", ParentId = 6, Size = 3072, Extension = "jpg" };

        context.Files.AddRange(file1, file2, file3, file4, file5);

        await context.SaveChangesAsync();
    }
}
