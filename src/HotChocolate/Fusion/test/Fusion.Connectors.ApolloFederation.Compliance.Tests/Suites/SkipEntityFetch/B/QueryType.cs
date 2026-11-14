using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

// The audit "b" subgraph declares no root field.
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Name(OperationTypeNames.Query);
    }
}
