using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal class FieldScopedVariableResolver
        : IScopedVariableResolver
    {
        public VariableValue Resolve(
            IMiddlewareContext context,
            IReadOnlyDictionary<string, object> variables,
            ScopedVariableNode variable)
        {
            if (context.ObjectType.Fields.TryGetField(variable.Name.Value,
                out ObjectField field))
            {
                var obj = context.Parent<IReadOnlyDictionary<string, object>>();

                return new VariableValue
                (
                    variable.ToVariableName(),
                    field.Type.ToTypeNode(),
                    obj[field.Name],
                    null
                );
            }

            throw new QueryException(QueryError.CreateFieldError(
                $"A field with the name `{variable.Name.Value}` " +
                "does not exist.",
                context.Path,
                context.FieldSelection)
                .WithCode(ErrorCodes.FieldNotDefined));
        }
    }
}
