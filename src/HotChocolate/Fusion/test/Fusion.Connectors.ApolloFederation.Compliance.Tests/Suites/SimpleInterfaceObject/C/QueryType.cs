using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

/// <summary>
/// Root <c>Query</c> for the <c>c</c> subgraph. The audit SDL declares no
/// user-facing root fields on <c>c</c>; the subgraph contributes only the
/// <c>Account @interfaceObject</c> entity and the federation
/// <c>_service</c> / <c>_entities</c> fields.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
    }
}
