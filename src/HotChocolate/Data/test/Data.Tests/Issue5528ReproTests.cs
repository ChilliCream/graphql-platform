using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue5528ReproTests
{
    [Fact]
    public async Task List_Of_Union_With_Fluent_Api_And_Projection_Does_Not_Throw()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<Query>()
            .AddType<TenantType>()
            .AddType<FileEntryType>()
            .AddType<FolderEntryType>()
            .AddType<FileOrFolderUnionType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              tenants {
                id
                entries {
                  ... on FileEntry {
                    name
                    fileSize
                  }
                  ... on FolderEntry {
                    name
                    childCount
                  }
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    public class Query
    {
        [UseProjection]
        public IQueryable<Tenant> GetTenants()
            => s_data.AsQueryable();
    }

    public class Tenant
    {
        public int Id { get; set; }

        public List<Entry> Entries { get; set; } = [];
    }

    public abstract class Entry
    {
        public string Name { get; set; } = string.Empty;
    }

    public class FileEntry : Entry
    {
        public int FileSize { get; set; }
    }

    public class FolderEntry : Entry
    {
        public int ChildCount { get; set; }
    }

    public class TenantType : ObjectType<Tenant>
    {
        protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
        {
            descriptor.Field(t => t.Entries).Type<ListType<FileOrFolderUnionType>>();
        }
    }

    public class FileEntryType : ObjectType<FileEntry>;

    public class FolderEntryType : ObjectType<FolderEntry>;

    public class FileOrFolderUnionType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("FileOrFolder");
            descriptor.Type<FileEntryType>();
            descriptor.Type<FolderEntryType>();
        }
    }

    private static readonly Tenant[] s_data =
    [
        new()
        {
            Id = 1,
            Entries =
            [
                new FileEntry
                {
                    Name = "README.md",
                    FileSize = 123
                },
                new FolderEntry
                {
                    Name = "src",
                    ChildCount = 3
                }
            ]
        }
    ];
}
