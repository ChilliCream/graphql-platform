using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal class ArgumentScopedVariableResolver
        : IScopedVariableResolver
    {
        public VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            if (!ScopeNames.Arguments.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(
                    "This resolver can only handle argument scopes.",
                    nameof(variable));
            }

            InputField argument = context.Field.Arguments.FirstOrDefault(t =>
                t.Name.Value.EqualsOrdinal(variable.Name.Value));

            if (argument == null)
            {
                throw new QueryException(QueryError.CreateFieldError(
                    $"An argument with the name `{variable.Name.Value}` " +
                    "does not exist.",
                    context.Path,
                    context.FieldSelection)
                    .WithCode(ErrorCodes.ArgumentNotDefined));
            }

            return new VariableValue
            (
                variable.ToVariableName(),
                argument.Type.ToTypeNode(),
                context.Argument<object>(variable.Name.Value),
                argument.Type.IsNonNullType()
                    && argument.DefaultValue.IsNull()
                    ? null
                    : argument.DefaultValue
            );
        }
    }
}
