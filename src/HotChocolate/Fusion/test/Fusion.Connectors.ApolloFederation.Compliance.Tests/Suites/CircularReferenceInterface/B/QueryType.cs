using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CircularReferenceInterface.B;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
    }
}
