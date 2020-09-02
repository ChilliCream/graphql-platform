using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal delegate object InputObjectFactory(
        IReadOnlyDictionary<string, object?> fieldValues,
        ITypeConverter converter);
}
