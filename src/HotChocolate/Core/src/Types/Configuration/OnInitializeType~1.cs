using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public delegate void OnInitializeType<T>(
        ITypeDiscoveryContext context,
        T? definition,
        IDictionary<string, object?> contextData)
        where T : DefinitionBase;
}