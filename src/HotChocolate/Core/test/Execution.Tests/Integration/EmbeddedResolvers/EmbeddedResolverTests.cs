using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.EmbeddedResolvers;

public class EmbeddedResolverTests
{
    [Fact]
    public async Task ResolverResultIsObject()
    {
        await ExpectValid(
                "{ foo { bar { baz }}}",
                configure: c => c.AddQueryType<QueryType>())
            .MatchSnapshotAsync();
    }

    public class QueryType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field<QueryType>(t => t.GetFoo()).Type<FooType>();
        }

        public object GetFoo()
        {
            return new object();
        }
    }

    public class FooType
        : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field<FooType>(t => t.Bar()).Type<BarType>();
        }

        public Bar Bar()
        {
            return new Bar();
        }
    }

    public class BarType
        : ObjectType<Bar>
    {
        protected override void Configure(IObjectTypeDescriptor<Bar> descriptor)
        {
        }
    }

    public class Bar
    {
        public string Baz { get; } = "Bar";
    }
}
