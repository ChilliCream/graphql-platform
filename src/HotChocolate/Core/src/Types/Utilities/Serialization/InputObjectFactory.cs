using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal delegate object InputObjectFactory(
        IReadOnlyDictionary<string, object?> fieldValues,
        ITypeConverter converter);
}
