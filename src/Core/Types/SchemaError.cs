using System;
using System.Collections.Generic;
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

    public interface ISchemaError
    {
        /// <summary>
        /// Gets the error message.
        /// This property is mandatory and cannot be null.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets an error code that can be used to automatically
        /// process an error.
        /// This property is optional and can be null.
        /// </summary>
        string Code { get; }

        ITypeSystemObject TypeSystemObject { get; }

        /// <summary>
        /// Gets the path to the object that caused the error.
        /// This property is optional and can be null.
        /// </summary>
        IReadOnlyCollection<object> Path { get; }


        IReadOnlyCollection<ISyntaxNode> SyntaxNodes { get; }

        /// <summary>
        /// Gets the exception associated with this error.
        /// </summary>
        Exception Exception { get; }
    }
}
