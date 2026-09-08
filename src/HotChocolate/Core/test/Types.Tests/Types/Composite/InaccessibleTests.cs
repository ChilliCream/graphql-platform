using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class InaccessibleTests
{
    [Fact]
    public static async Task Inaccessible_Should_ApplyDirective_When_UsedOnGenericObjectTypeDescriptor()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddType<GenericInaccessibleType>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        public GenericInaccessible GetInaccessible() => new();
    }

    public class GenericInaccessible
    {
        public string? Field { get; set; }
    }

    // fluent authoring of the @inaccessible directive through IObjectTypeDescriptor<T>,
    // without casting to the non-generic IObjectTypeDescriptor.
    public class GenericInaccessibleType : ObjectType<GenericInaccessible>
    {
        protected override void Configure(IObjectTypeDescriptor<GenericInaccessible> descriptor)
        {
            descriptor.Inaccessible();
        }
    }
}
