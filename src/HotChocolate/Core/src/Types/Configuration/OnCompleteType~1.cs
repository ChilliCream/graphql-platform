using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
{
    public delegate void OnCompleteType<T>(
        ITypeCompletionContext context,
        T? definition,
        IDictionary<string, object?> contextData)
        where T : DefinitionBase;
}