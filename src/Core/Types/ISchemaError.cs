using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class SchemaError
    {
        public SchemaError(string message, INamedType type)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Message = message;
            Type = type;
        }

        public SchemaError(string message, INamedType type, ISyntaxNode syntaxNode)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Message = message;
            Type = type;
            SyntaxNode = syntaxNode;
        }

        public string Message { get; }
        public INamedType Type { get; }
        public ISyntaxNode SyntaxNode { get; }
    }
}
