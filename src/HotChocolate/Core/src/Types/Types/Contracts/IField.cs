using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types
{
    public interface IField
        : IHasName
        , IHasDescription
        , IHasDirectives
        , IHasSyntaxNode
        , IHasRuntimeType
    {
        /// <summary>
        /// The type of which declares this field.
        /// </summary>
        ITypeSystemObject DeclaringType { get; }

        IReadOnlyDictionary<string, object?> ContextData { get; }
    }
}
