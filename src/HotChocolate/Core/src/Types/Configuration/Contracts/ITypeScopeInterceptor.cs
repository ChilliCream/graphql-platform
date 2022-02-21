using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

public interface ITypeScopeInterceptor
{
    bool TryCreateScope(
        ITypeDiscoveryContext discoveryContext,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? typeDependencies);
}
