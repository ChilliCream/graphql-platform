using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.C;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>c</c> subgraph. The subgraph
/// declares no user-facing query fields. The federation infrastructure
/// (<c>_service</c>, <c>_entities</c>) is stripped during composition, so a
/// single inaccessible <c>_noop</c> field is added to keep the <c>Query</c>
/// type non-empty for source schema validation.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("_noop")
            .Type<StringType>()
            .Inaccessible()
            .Resolve(_ => (string?)null);
    }
}
