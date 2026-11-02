using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class CommonType : ObjectType<Common>
{
    protected override void Configure(IObjectTypeDescriptor<Common> descriptor)
    {
        descriptor.Shareable();
        descriptor.Field(c => c.Label).Type<StringType>();
    }
}
