using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class UsePagingAttributeTests
    {
        [Fact]
        public async Task UsePagingAttribute_Infer_Types()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task UsePagingAttribute_Execute_Query()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .Services
                .BuildServiceProvider()
                .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task UsePagingAttribute_Infer_Types_On_Interface()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddType<IHasFoos>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task UsePagingAttribute_On_Extension_Infer_Types()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddType<QueryExtension>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task UsePagingAttribute_On_Extension_Execute_Query()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .AddType<QueryExtension>()
                .Services
                .BuildServiceProvider()
                .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Ensure_Attributes_Are_Applied_Once()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<Query1Extensions>()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Ensure_Attributes_Are_Applied_Once_Execute_Query()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddType<Query1Extensions>()
                .ExecuteRequestAsync("{ foos(first: 1) { nodes { bar } } }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task UnknownNodeType()
        {
            Snapshot.FullName();

            try
            {
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryType>()
                    .AddType<NoNodeType>()
                    .BuildSchemaAsync();
            }
            catch (SchemaException ex)
            {
                new
                {
                    ex.Errors[0].Message,
                    ex.Errors[0].Code
                }.MatchSnapshot();
            }
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
            }
        }

        public class Query
        {
            [UsePaging]
            public IQueryable<Foo> Foos ()
            {
                return new List<Foo>
                {
                    new Foo { Bar = "first" },
                    new Foo { Bar = "second" },
                }.AsQueryable();
            }
        }

        public class Query1
        {
            public IQueryable<Foo> Foos ()
            {
                return new List<Foo>
                {
                    new Foo { Bar = "first" },
                    new Foo { Bar = "second" },
                }.AsQueryable();
            }
        }

        [Node]
        [ExtendObjectType(typeof(Query1))]
        public class Query1Extensions
        {
            [UsePaging]
            [BindProperty(nameof(Query1.Foos))]
            public IQueryable<Foo> Foos ()
            {
                return new List<Foo>
                {
                    new Foo { Bar = "first" },
                    new Foo { Bar = "second" },
                }.AsQueryable();
            }

            [NodeResolver]
            public Query1 GetQuery()
            {
                return new Query1();
            }
        }

        [ExtendObjectType(Name = "Query")]
        public class QueryExtension : Query
        {
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public interface IHasFoos
        {
            [UsePaging]
            IQueryable<Foo> Foos { get; }
        }

        [ExtendObjectType(Name = "Query")]
        public class NoNodeType
        {
            [UsePaging]
            public int GetSomething() => 1;
        }
    }
}
