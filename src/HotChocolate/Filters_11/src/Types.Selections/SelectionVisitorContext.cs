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
        private readonly IResolverContext _context;

        public SelectionVisitorContext(
            IResolverContext context,
            ITypeConversion conversion,
            IPreparedSelection fieldSelection,
            SelectionMiddlewareContext selectionMiddlewareContext)
        {
            Conversion = conversion;
            FieldSelection = fieldSelection;
            SelectionContext = selectionMiddlewareContext;
            _context = context;
            _arguments = CoerceArguments(
                fieldSelection.Arguments, 
                context.Variables, 
                context.Path);

        }

        public ITypeConversion Conversion { get; }

        public IPreparedSelection FieldSelection { get; }

        public SelectionMiddlewareContext SelectionContext { get; }

        public bool TryGetValueNode(string key, [NotNullWhen(true)] out IValueNode? value)
        {
            if (_arguments.TryGetValue(key, out PreparedArgument? argument) &&
                argument.ValueLiteral is { } &&
                argument.ValueLiteral.Kind != NodeKind.NullValue)
            {
                value = argument.ValueLiteral;
                return true;
            }
            value = null;
            return false;
        }

        public void ReportErrors(IList<IError> errors)
        {
            foreach (IError error in errors)
            {
                SelectionContext.Errors.Add(
                    error.WithPath(_context.Path));
            }
        }

        private static IReadOnlyDictionary<NameString, PreparedArgument> CoerceArguments(
            IPreparedArgumentMap arguments,
            IVariableValueCollection variables,
            Path path)
        {
            if (arguments.HasErrors)
            {
                var errors = new List<IError>();

                foreach (PreparedArgument argument in arguments.Values)
                {
                    if (argument.IsError)
                    {
                        errors.Add(argument.Error!.WithPath(path));
                    }
                }

                throw new GraphQLException(errors);
            }

            if (arguments.IsFinal)
            {
                return arguments;
            }

            var args = new Dictionary<NameString, PreparedArgument>();

            foreach (PreparedArgument argument in arguments.Values)
            {
                if (argument.IsFinal)
                {
                    args.Add(argument.Argument.Name, argument);
                }
                else
                {
                    IValueNode literal = VariableRewriter.Rewrite(
                        argument.ValueLiteral!, variables);

                    args.Add(argument.Argument.Name, new PreparedArgument(
                        argument.Argument,
                        literal.TryGetValueKind(out ValueKind kind)
                            ? kind
                            : ValueKind.Unknown,
                        argument.IsFinal,
                        argument.IsImplicit,
                        null,
                        literal));
                }
            }

            return args;
        }
    }
}
