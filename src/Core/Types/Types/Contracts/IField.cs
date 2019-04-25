using System.Collections.Generic;

namespace HotChocolate.Types
{
    public interface IField
        : IHasName
        , IHasDescription
        , IHasDirectives
    {
        /// <summary>
        /// The type of which declares this field.
        /// </summary>
        ITypeSystemObject DeclaringType { get; }

        IReadOnlyDictionary<string, object> ContextData { get; }
    }
}
