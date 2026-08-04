using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class AlphaType : ObjectType<Alpha>
{
    protected override void Configure(IObjectTypeDescriptor<Alpha> descriptor)
    {
        descriptor.Shareable();

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Value).Type<StringType>();
    }
}
