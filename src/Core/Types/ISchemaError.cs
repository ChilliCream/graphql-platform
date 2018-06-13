using System;
using HotChocolate.Language;

namespace HotChocolate.Types
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
        }

        public string Message { get; }
        public INamedType Type { get; }
        public ISyntaxNode SyntaxNode { get; }
        public Exception AssociatedException { get; }
    }
}
