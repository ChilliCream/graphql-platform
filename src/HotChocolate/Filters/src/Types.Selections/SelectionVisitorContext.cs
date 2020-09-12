using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionVisitorContext
    {
        private readonly IReadOnlyDictionary<NameString, PreparedArgument> _arguments;

        public SelectionVisitorContext(
            IResolverContext context,
            ITypeConverter conversion,
            IPreparedSelection fieldSelection,
            SelectionMiddlewareContext selectionMiddlewareContext)
        {
            Conversion = conversion;
            FieldSelection = fieldSelection;
            SelectionContext = selectionMiddlewareContext;
            _arguments = CoerceArguments(
                fieldSelection.Arguments,
                context.Variables);
        }

        public ITypeConverter Conversion { get; }

        public IPreparedSelection FieldSelection { get; }

        public SelectionMiddlewareContext SelectionContext { get; }

        public bool TryGetValueNode(string key, [NotNullWhen(true)]out IValueNode? value)
        {
            if (_arguments.TryGetValue(key, out PreparedArgument? argument) &&
                argument.ValueLiteral is { } &&
                argument.ValueLiteral.Kind != SyntaxKind.NullValue)
            {
                value = argument.ValueLiteral;
                return true;
            }
            value = null;
            return false;
        }

        private static IReadOnlyDictionary<NameString, PreparedArgument> CoerceArguments(
            IPreparedArgumentMap arguments,
            IVariableValueCollection variables)
        {
            List<IError>? errors = null;

            void ReportError(IError c) =>
                (errors ??= new List<IError>()).Add(c);

            if (arguments.TryCoerceArguments(
                variables,
                ReportError,
                out IReadOnlyDictionary<NameString, PreparedArgument>? coercedArgs))
            {
                return coercedArgs;
            }

            throw new GraphQLException(errors);
        }
    }
}
