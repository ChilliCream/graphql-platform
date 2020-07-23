using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionVisitorContext
    {
        private readonly IResolverContext _context;
        private IReadOnlyDictionary<NameString, PreparedArgument> _arguments;

        public SelectionVisitorContext(
            IResolverContext context,
            ITypeConversion conversion,
            IPreparedSelection fieldSelection)
        {
            Conversion = conversion;
            FieldSelection = fieldSelection;
            _context = context;
            
        }

        public ITypeConversion Conversion { get; }

        public IPreparedSelection FieldSelection { get; }

        public bool TryGetValueNode(string key, out IValueNode arg)
        {
            PreCoerceArguments();

            if (_arguments.TryGetValue(key, out PreparedArgument argumentValue) &&
                argumentValue.ValueLiteral != null &&
                !(argumentValue.ValueLiteral is NullValueNode))
            {
                IValueNode literal = argumentValue.ValueLiteral;

                arg = VariableToValueRewriter.Rewrite(
                    literal,
                    argumentValue.Type,
                     _context.Variables, Conversion);

                return true;
            }
            arg = null;
            return false;
        }

        private void PreCoerceArguments()
        {
            if (_arguments is null)
            {
                List<IError> errors = null;

                if (!FieldSelection.Arguments.TryCoerceArguments(
                    _context.Variables,
                    error =>
                    {
                        if (errors is null)
                        {
                            errors = new List<IError>();
                        }
                        errors.Add(error.WithPath(_context.Path));
                    },
                    out _arguments))
                {
                    throw new QueryException(errors);
                }
            }
        }
    }
}
