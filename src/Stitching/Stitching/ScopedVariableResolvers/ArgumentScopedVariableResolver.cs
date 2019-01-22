using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal class ArgumentScopedVariableResolver
        : IScopedVariableResolver
    {
        public VariableValue Resolve(
            IMiddlewareContext context,
            IReadOnlyDictionary<string, object> variables,
            ScopedVariableNode variable)
        {
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
                argument.DefaultValue
            );
        }
    }
}
