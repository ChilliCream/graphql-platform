using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Selections
{
    public class SelectionVisitorContext
    {
        private readonly IReadOnlyDictionary<NameString, ArgumentValue> _arguments;
        private readonly IResolverContext _context;

        public SelectionVisitorContext(
            IResolverContext context,
            ITypeConversion conversion,
            FieldSelection fieldSelection)
        {
            Conversion = conversion;
            FieldSelection = fieldSelection;
            _context = context;
            _arguments = fieldSelection.CoerceArguments(context.Variables, conversion);
        }

        public ITypeConversion Conversion { get; }

        public FieldSelection FieldSelection { get; }

        public bool TryGetValueNode(string key, out IValueNode arg)
        {
            if (_arguments.TryGetValue(key, out ArgumentValue argumentValue) &&
                argumentValue.Literal != null &&
                !(argumentValue.Literal is NullValueNode))
            {
                EnsureNoError(argumentValue);

                IValueNode literal = argumentValue.Literal;

                arg = VariableToValueRewriter.Rewrite(
                    literal,
                    argumentValue.Type,
                     _context.Variables, Conversion);

                return true;
            }
            arg = null;
            return false;
        }

        protected void EnsureNoError(ArgumentValue argumentValue)
        {
            if (argumentValue.Error != null)
            {
                throw new QueryException(argumentValue.Error);
            }
        }
    }
}
