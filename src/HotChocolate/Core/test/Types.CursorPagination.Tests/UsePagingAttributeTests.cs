using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Tests;
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
            public IQueryable<Foo> Foos { get; set; } = new List<Foo>
            {
                new Foo { Bar = "first" },
                new Foo { Bar = "second" },
            }.AsQueryable();
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
    }
}
