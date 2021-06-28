using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Simple_StringList_Schema()
        {
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            executor.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Attribute_Simple_StringList_Schema()
        {
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            executor.Schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Simple_StringList_Default_Items()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters {
                        items
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

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Take_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Simple_StringList_Take_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Take_2_Skip_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2 skip: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Simple_StringList_Take_2_Skip_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2 skip: 2) {
                        items
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

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 2 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Global_DefaultItem_50_Page_Larger_Than_Data_List()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 50 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters {
                        items
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

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .SetPagingOptions(new PagingOptions { DefaultPageSize = 2 })
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Schema_Type_Is_Explicitly_Specified()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    explicitType(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Schema_Type_Is_Explicitly_Specified()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    explicitType(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Nested_List_With_Field_Settings()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    nestedObjectList {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Executable_With_Field_Settings()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<ExecutableQueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    fooExecutable {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Nested_List_With_Field_Settings()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    nestedObjectList {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Nested_List_With_Field_Settings_Skip_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    nestedObjectList(skip: 2) {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Attribute_Nested_List_With_Field_Settings_Skip_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .Services
                    .BuildServiceProvider()
                    .GetRequestExecutorAsync();

            await executor
                .ExecuteAsync(@"
                {
                    nestedObjectList(skip: 2) {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Interface_With_Paging_Field()
        {
            Snapshot.FullName();

            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .AddInterfaceType<ISome>(d => d
                        .Field(t => t.ExplicitType())
                        .UseOffsetPaging())
                    .ModifyOptions(o =>
                    {
                        o.RemoveUnreachableTypes = false;
                        o.StrictValidation = false;
                    })
                    .Services
                    .BuildServiceProvider()
                    .GetSchemaAsync();

            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Attribute_Interface_With_Paging_Field()
        {
            Snapshot.FullName();

            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryAttr>()
                    .AddInterfaceType<ISome2>()
                    .ModifyOptions(o =>
                    {
                        o.RemoveUnreachableTypes = false;
                        o.StrictValidation = false;
                    })
                    .Services
                    .BuildServiceProvider()
                    .GetSchemaAsync();

            schema.Print().MatchSnapshot();
        }

        public class QueryType : ObjectType<Query>
        {
            protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor
                    .Field(t => t.Letters)
                    .UseOffsetPaging();

                descriptor
                    .Field("explicitType")
                    .ResolveWith<Query>(t => t.Letters)
                    .UseOffsetPaging<NonNullType<StringType>>();

                descriptor
                    .Field(t => t.Foos())
                    .Name("nestedObjectList")
                    .UseOffsetPaging(
                        options: new PagingOptions
                        {
                            MaxPageSize = 2,
                            IncludeTotalCount = true
                        });
            }
        }

        public class ExecutableQueryType : ObjectType<ExecutableQuery>
        {
            protected override void Configure(IObjectTypeDescriptor<ExecutableQuery> descriptor)
            {
                descriptor
                    .Field(t => t.FoosExecutable())
                    .Name("fooExecutable")
                    .UseOffsetPaging(
                        options: new PagingOptions
                        {
                            MaxPageSize = 2,
                            IncludeTotalCount = true
                        });
            }
        }

        public class Query
        {
            public string[] Letters => new[]
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l"
            };

            public List<List<Foo>> Foos() => new List<List<Foo>>
            {
                new List<Foo> { new Foo { Bar = "a" } },
                new List<Foo> { new Foo { Bar = "b" }, new Foo { Bar = "c" } },
                new List<Foo> { new Foo { Bar = "d" } },
                new List<Foo> { new Foo { Bar = "e" } },
                new List<Foo> { new Foo { Bar = "f" } }
            };
        }

        public class ExecutableQuery
        {
            public IExecutable<Foo> FoosExecutable() => new MockExecutable<Foo>(new List<Foo>
            {
                  new Foo { Bar = "a" },
                  new Foo { Bar = "b" },
                  new Foo { Bar = "c" } ,
                  new Foo { Bar = "d" },
                  new Foo { Bar = "e" },
                  new Foo { Bar = "f" }
            }.AsQueryable());
        }

        public class Foo
        {
            public string Bar { get; set; } = default!;
        }

        public class QueryAttr
        {
            [UseOffsetPaging]
            public string[] Letters => new[]
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l"
            };

            [UseOffsetPaging(typeof(NonNullType<StringType>))]
            public string[] ExplicitType => Letters;

            [GraphQLName("nestedObjectList")]
            [UseOffsetPaging(
                MaxPageSize = 2,
                IncludeTotalCount = true)]
            public List<List<Foo>> Foos() => new List<List<Foo>>
            {
                new List<Foo> { new Foo { Bar = "a" } },
                new List<Foo> { new Foo { Bar = "b" }, new Foo { Bar = "c" } },
                new List<Foo> { new Foo { Bar = "d" } },
                new List<Foo> { new Foo { Bar = "e" } },
                new List<Foo> { new Foo { Bar = "f" } }
            };
        }

        public interface ISome
        {
            public string[] ExplicitType();
        }

        public interface ISome2
        {
            [UseOffsetPaging(typeof(NonNullType<StringType>))]
            public string[] ExplicitType();
        }
    }

    public class MockExecutable<T> : IExecutable<T>
    {
        private readonly IQueryable<T> _source;

        public MockExecutable(IQueryable<T> source)
        {
            _source = source;
        }

        public object Source =>_source;

        public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<IList>(_source.ToList());
        }

        public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<object?>(_source.FirstOrDefault());
        }

        public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<object?>(_source.SingleOrDefault());
        }

        public string Print()
        {
            return _source.ToString();
        }
    }
}
