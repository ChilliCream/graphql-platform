using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public class SchemaError
    {
        public SchemaError(
            string message,
            Exception associatedException = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            Message = message;
            AssociatedException = associatedException;
        }

        public SchemaError(
            string message, INamedType type,
            Exception associatedException = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            Message = message;
            Type = type;
            AssociatedException = associatedException;
        }

        public SchemaError(
            string message, INamedType type,
            ISyntaxNode syntaxNode,
            Exception associatedException = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            Message = message;
            Type = type;
            SyntaxNode = syntaxNode;
            AssociatedException = associatedException;
        }

        public string Message { get; }
        public INamedType Type { get; }
        public ISyntaxNode SyntaxNode { get; }
        public Exception AssociatedException { get; }
    }
}
