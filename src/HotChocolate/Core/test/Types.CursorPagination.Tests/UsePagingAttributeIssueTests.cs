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
    public class UsePagingAttributeIssueTests
    {
        [Fact]
        public async Task UsePagingAttribute_Issue()
        {
            Snapshot.FullName();

            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<FooType>()
                .AddType<BarType>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class Query
        {
            public Foo Foo()
            {
                return new Foo();
            }

            public Bar Bar()
            {
                return new Bar();
            }
        }

        public class FooType : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                base.Configure(descriptor);
                descriptor.Field("bazzes")
                    .UsePaging<ObjectType<Bazz>>()
                    .Resolve(ctx =>
                        new List<Bazz> { new Bazz { }, new Bazz {} }.AsQueryable());
            }
        }

        public class BarType : ObjectType<Bar>
        {
            protected override void Configure(IObjectTypeDescriptor<Bar> descriptor)
            {
                base.Configure(descriptor);
                descriptor.Field("bazzes")
                    .UsePaging<ObjectType<BazzSummary>>()
                    .Resolve(ctx =>
                        new List<BazzSummary> { new BazzSummary { }, new BazzSummary {} }.AsQueryable());

                descriptor.Field("bazzSummaries")
                    .UsePaging<ObjectType<BazzSummary>>()
                    .Resolve(ctx =>
                        new List<BazzSummary> { new BazzSummary { }, new BazzSummary {} }.AsQueryable());
            }
        }

        public class Foo
        {
        }

        public class Bar
        {
            public string Qux { get; set; }
        }

        public class Bazz
        {
            public string Field1 { get; set; }
            public string ExpensiveField2 { get; set; }
        }

        public class BazzSummary
        {
            public string Field1 { get; set; }
        }
    }
}
