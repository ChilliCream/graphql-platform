using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class InternalTests
{
    [Fact]
    public static async Task Internal_Should_ApplyDirective_When_UsedOnGenericObjectTypeDescriptor()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<GenericInternalType>()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        public GenericInternal GetInternal() => new();
    }

    public class GenericInternal
    {
        public string? Field { get; set; }
    }

    // fluent authoring of the @internal directive through IObjectTypeDescriptor<T>,
    // without casting to the non-generic IObjectTypeDescriptor.
    public class GenericInternalType : ObjectType<GenericInternal>
    {
        protected override void Configure(IObjectTypeDescriptor<GenericInternal> descriptor)
        {
            descriptor.Internal();
        }
    }
}
