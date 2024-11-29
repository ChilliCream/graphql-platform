using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public static class InterfaceTypeResolverTests
{
    [Fact]
    public static async Task InheritInterface()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddType<QueryType>()
                .AddType<SomeObjectType>()
                .ExecuteRequestAsync("{ some { field } }");

        result.MatchSnapshot();
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("some").Type<SomeInterfaceType>().Resolve(_ => new SomeObject());
        }
    }

    public class SomeInterfaceType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("SomeInterface");
            descriptor.Field("field").Type<StringType>().Resolve(_ => new ValueTask<object>("Test"));
        }
    }

    public class SomeObjectType : ObjectType<SomeObject>
    {
        protected override void Configure(
            IObjectTypeDescriptor<SomeObject> descriptor)
        {
            descriptor.Name("SomeObject");
            descriptor.Implements<SomeInterfaceType>();
        }
    }

    public class SomeObject;
}
