using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Stitching.ThrowHelper;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables;

internal class ArgumentScopedVariableResolver : IScopedVariableResolver
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

        if (!ScopeNames.Arguments.Equals(variable.Scope.Value))
        {
            throw new ArgumentException(
                ArgumentScopedVariableResolver_CannotHandleVariable,
                nameof(variable));
        }

        if (!context.Selection.Field.Arguments.TryGetField(
            variable.Name.Value,
            out var argument))
        {
            throw ArgumentScopedVariableResolver_InvalidArgumentName(
                variable.Name.Value,
                context.Selection.SyntaxNode,
                context.Path);
        }

        return new ScopedVariableValue
        (
            variable.ToVariableName(),
            argument.Type.ToTypeNode(),
            context.ArgumentLiteral<IValueNode>(variable.Name.Value),
            argument.Type.IsNonNullType() && argument.DefaultValue.IsNull()
                ? null
                : argument.DefaultValue
        );
    }
}
