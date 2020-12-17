using System;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Stitching.ThrowHelper;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation.ScopedVariables
{
    internal class ArgumentScopedVariableResolver : IScopedVariableResolver
    {
        public VariableValue Resolve(
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

            IInputField? argument = context.Field.Arguments.FirstOrDefault(
                t => t.Name.Value.EqualsOrdinal(variable.Name.Value));

            if (argument is null)
            {
                throw ArgumentScopedVariableResolver_InvalidArgumentName(
                    variable.Name.Value,
                    context.Selection.SyntaxNode,
                    context.Path);
            }

            return new VariableValue
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
}
