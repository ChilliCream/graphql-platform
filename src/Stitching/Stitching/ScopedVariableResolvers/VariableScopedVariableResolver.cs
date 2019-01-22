using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    internal class VariableScopedVariableResolver
        : IScopedVariableResolver
    {
        public VariableValue Resolve(
            IMiddlewareContext context,
            IReadOnlyDictionary<string, object> variables,
            ScopedVariableNode variable)
        {
            VariableDefinitionNode definition =
                context.Operation.VariableDefinitions.FirstOrDefault(t =>
                    t.Variable.Name.Value.EqualsOrdinal(variable.Name.Value));

            if (definition == null)
            {
                throw new QueryException(QueryError.CreateFieldError(
                    $"A variable with the name `{variable.Name.Value}` " +
                    "does not exist.",
                    context.Path,
                    context.FieldSelection)
                    .WithCode(ErrorCodes.VariableNotDefined));
            }

            variables.TryGetValue(variable.Name.Value, out object value);

            return new VariableValue
            (
                definition.Variable.Name.Value,
                definition.Type,
                value,
                definition.DefaultValue
            );
        }
    }
}
