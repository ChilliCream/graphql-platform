using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Validation
{
    public class ValidationError
         : QueryError
    {
        public ValidationError(string message, Language.ISyntaxNode syntaxNode)
            : base(message)
        {
            if (syntaxNode == null)
            {
                throw new ArgumentNullException(nameof(syntaxNode));
            }

            Locations = new[]
            {
                new Location(
                    syntaxNode.Location.StartToken.Line,
                    syntaxNode.Location.StartToken.Column)
            };
        }

        public ValidationError(string message, IEnumerable<Language.ISyntaxNode> syntaxNodes)
           : base(message)
        {
            if (syntaxNodes == null)
            {
                throw new ArgumentNullException(nameof(syntaxNodes));
            }

            Locations = syntaxNodes.Select(t => new Location(
                t.Location.StartToken.Line,
                t.Location.StartToken.Column)).ToArray();
        }

        public IReadOnlyCollection<Location> Locations { get; }
    }
}
