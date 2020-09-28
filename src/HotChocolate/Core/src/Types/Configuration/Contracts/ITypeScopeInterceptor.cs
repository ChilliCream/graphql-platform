using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeScopeInterceptor
    {
        bool TryCreateScope(
            ITypeDiscoveryContext discoveryContext,
            [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies);
    }
}
