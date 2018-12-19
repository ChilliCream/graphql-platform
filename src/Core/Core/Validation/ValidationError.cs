using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Validation
{
    public class ValidationError
         : QueryError
    {
        public ValidationError(
            string message,
            Language.ISyntaxNode syntaxNode)
            : this(message, new[] { syntaxNode })
        {
        }

        public ValidationError(
            string message,
            IEnumerable<Language.ISyntaxNode> syntaxNodes)
            : this(message, syntaxNodes?.ToArray())
        {
        }

        public ValidationError(
            string message,
            params Language.ISyntaxNode[] syntaxNodes)
            : base(message, CreateLocations(syntaxNodes))
        {
        }
    }
}
