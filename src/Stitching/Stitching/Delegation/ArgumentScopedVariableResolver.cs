using System;
using System.Globalization;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Properties;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Delegation
{
    internal class ArgumentScopedVariableResolver
        : IScopedVariableResolver
    {
        public VariableValue Resolve(
            IResolverContext context,
            ScopedVariableNode variable,
            ITypeNode targetType)
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
                throw new ArgumentException(StitchingResources
                    .ArgumentScopedVariableResolver_CannotHandleVariable,
                    nameof(variable));
            }

            InputField argument = context.Field.Arguments.FirstOrDefault(t =>
                t.Name.Value.EqualsOrdinal(variable.Name.Value));

            if (argument == null)
            {
                throw new QueryException(QueryError.CreateFieldError(
                    string.Format(CultureInfo.InvariantCulture,
                        StitchingResources
                            .ArgumentScopedVariableResolver_InvalidArgumentName,
                        variable.Name.Value),
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
