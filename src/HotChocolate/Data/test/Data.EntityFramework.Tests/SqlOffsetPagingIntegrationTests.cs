using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class SqlOffsetPagingIntegrationTests : SqlLiteOffsetTestBase
{
    public TestData[] Data => new[]
    {
        new TestData(Guid.NewGuid(), "A"),
        new TestData(Guid.NewGuid(), "B"),
        new TestData(Guid.NewGuid(), "C"),
        new TestData(Guid.NewGuid(), "D")
    };

    [Fact]
    public async Task Simple_StringList_Default_Items()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task No_Boundaries_Set()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Default_Items()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_Skip_2()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root(take: 2) {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Skip_2()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root(take: 2) {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_Skip_2_After()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root(take: 2) {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Skip_2_After()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root(take: 2) {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_Global_DefaultItem_2()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Global_DefaultItem_2()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        items {
                            foo
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TotalCount_Should_Be_Correct()
    {
        Snapshot.FullName();

        IRequestExecutor executor = CreateSchema(Data);

        await executor
            .ExecuteAsync(@"
                {
                    root {
                        totalCount
                    }
                }")
            .MatchSnapshotAsync();
    }

    public class TestData
    {
        public TestData(Guid id, string foo)
        {
            Id = id;
            Foo = foo;
        }

        public Guid Id { get; set; }

        public string Foo { get; set; }
    }
}
