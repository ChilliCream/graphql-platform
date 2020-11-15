using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public delegate void OnCompleteType(
        ITypeCompletionContext context,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);
}