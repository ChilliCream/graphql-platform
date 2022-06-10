using System;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Processing.ScopedVariables;

internal class ScopedContextDataScopedVariableResolver
    : IScopedVariableResolver
{
    public ScopedVariableValue Resolve(
        IResolverContext context,
        ScopedVariableNode variable,
        IInputType targetType)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (variable is null)
        {
            throw new ArgumentNullException(nameof(variable));
        }

        if (targetType is null)
        {
            throw new ArgumentNullException(nameof(targetType));
        }

        if (!ScopeNames.ScopedContextData.Equals(variable.Scope.Value))
        {
            throw new ArgumentException(
                ScopedCtxDataScopedVariableResolver_CannotHandleVariable,
                nameof(variable));
        }

        context.ScopedContextData.TryGetValue(variable.Name.Value, out var data);
        InputFormatter formatter = context.Service<InputFormatter>();

        IValueNode literal = data switch
        {
            IValueNode l => l,
            null => NullValueNode.Default,
            _ => formatter.FormatValue(data, targetType, PathFactory.Instance.New(variable.Name.Value))
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
