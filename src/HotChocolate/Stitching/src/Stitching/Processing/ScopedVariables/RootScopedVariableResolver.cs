using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing.ScopedVariables;

internal class RootScopedVariableResolver
    : IScopedVariableResolver
{
    private readonly Dictionary<string, IScopedVariableResolver> _resolvers =
        new Dictionary<string, IScopedVariableResolver>
        {
                { ScopeNames.Arguments, new ArgumentScopedVariableResolver() },
                { ScopeNames.Fields, new FieldScopedVariableResolver() },
                { ScopeNames.ContextData, new ContextDataScopedVariableResolver() },
                { ScopeNames.ScopedContextData, new ScopedContextDataScopedVariableResolver() }
        };

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

        if (_resolvers.TryGetValue(variable.Scope.Value, out IScopedVariableResolver? resolver))
        {
            return resolver.Resolve(context, variable, targetType);
        }

        throw ThrowHelper.RootScopedVariableResolver_ScopeNotSupported(
            variable.Scope.Value,
            context.Selection.SyntaxNode,
            context.Path);
    }
}
