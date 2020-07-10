using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeInitializationInterceptor
    {
        bool CanHandle(ITypeSystemObjectContext context);

        void OnBeforeInitialize(
            ITypeDiscoveryContext context);

        void OnAfterInitialize(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);
    }

    public interface ITypeScopeInterceptor
    {
        bool TryCreateScope(
            ITypeDiscoveryContext discoveryContext,
            [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies);
    }

    public interface ITypeInterceptor
        : ITypeInitializationInterceptor
        , ITypeScopeInterceptor
    {
    }
}
