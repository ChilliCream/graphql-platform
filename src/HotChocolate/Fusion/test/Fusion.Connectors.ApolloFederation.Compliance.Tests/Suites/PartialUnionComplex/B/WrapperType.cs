using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnionComplex.B;

public sealed class WrapperType : ObjectType<Wrapper>
{
    protected override void Configure(IObjectTypeDescriptor<Wrapper> descriptor)
    {
        descriptor.Shareable();

        descriptor
            .Field(w => w.Actions)
            .Shareable()
            .Type<NonNullType<ListType<NonNullType<ActionUnionType>>>>();
    }
}
