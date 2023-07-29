using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class IntrospectionExecutionStep(
    int id,
    IObjectType queryType,
    ObjectTypeMetadata queryTypeMetadata)
    : ExecutionStep(id, null, queryType, queryTypeMetadata)
{
}
