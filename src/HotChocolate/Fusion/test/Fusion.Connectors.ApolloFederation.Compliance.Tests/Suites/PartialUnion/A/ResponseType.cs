using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.PartialUnion.A;

public sealed class ResponseType : ObjectType<Response>
{
    protected override void Configure(IObjectTypeDescriptor<Response> descriptor)
    {
        descriptor.Shareable();

        descriptor
            .Field(r => r.Actions)
            .Type<NonNullType<ListType<NonNullType<ActionUnionType>>>>();

        descriptor
            .Field(r => r.Message)
            .Type<StringType>();
    }
}
