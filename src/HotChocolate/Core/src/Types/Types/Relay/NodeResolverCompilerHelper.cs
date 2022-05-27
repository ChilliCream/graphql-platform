using HotChocolate.Internal;

namespace HotChocolate.Types.Relay;

internal static class NodeResolverCompilerHelper
{
    public static readonly IParameterExpressionBuilder[] ParameterExpressionBuilders =
    {
            NodeIdParameterExpressionBuilder.Instance
    };
}
