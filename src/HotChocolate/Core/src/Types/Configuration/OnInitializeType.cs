using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
{
    public delegate void OnInitializeType(
        IInitializationContext context,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);
}