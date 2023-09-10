using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables;

internal class ContextDataScopedVariableResolver : IScopedVariableResolver
{
    public ScopedVariableValue Resolve(
        IResolverContext context,
        ScopedVariableNode variable,
        IInputType targetType)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (targetType == null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (!ScopeNames.ContextData.Equals(variable.Scope.Value))
        {
            throw new ArgumentException(
                ContextDataScopedVariableResolver_CannotHandleVariable,
                nameof(variable));
        }

        context.ContextData.TryGetValue(variable.Name.Value, out var data);
        var formatter = context.Service<InputFormatter>();

        var literal = data switch
        {
            IValueNode l => l,
            null => NullValueNode.Default,
            _ => formatter.FormatValue(data, targetType, Path.Root.Append(variable.Name.Value))
        };

        return new ScopedVariableValue
        (
            variable.ToVariableName(),
            targetType.ToTypeNode(),
            literal,
            null
        );
    }
}
