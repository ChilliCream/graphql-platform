using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class SqlCursorPagingIntegrationTests : SqlLiteCursorTestBase
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
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
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
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
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
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_First_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor = CreateSchema(Data);

            await executor
                .ExecuteAsync(@"
                {
                    root(first: 2) {
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Simple_StringList_First_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor = CreateSchema(Data);

            await executor
                .ExecuteAsync(@"
                {
                    root(first: 2) {
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_First_2_After()
        {
            Snapshot.FullName();

            IRequestExecutor executor = CreateSchema(Data);

            await executor
                .ExecuteAsync(@"
                {
                    root(first: 2 after: ""MQ=="") {
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Simple_StringList_First_2_After()
        {
            Snapshot.FullName();

            IRequestExecutor executor = CreateSchema(Data);

            await executor
                .ExecuteAsync(@"
                {
                    root(first: 2 after: ""MQ=="") {
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
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
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
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
                        edges {
                            node {
                                foo
                            }
                            cursor
                        }
                        nodes {foo}
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
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
}
