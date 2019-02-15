using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Delegation
{
    internal class FieldScopedVariableResolver
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

            if (!ScopeNames.Fields.Equals(variable.Scope.Value))
            {
                throw new ArgumentException(
                    "This resolver can only handle field scopes.",
                    nameof(variable));
            }

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
