using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public delegate void OnCompleteType<T>(
        ITypeCompletionContext context,
        T? definition,
        IDictionary<string, object?> contextData)
        where T : DefinitionBase;
}