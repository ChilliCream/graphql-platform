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
            ITypeDiscoveryContext discoveryContext);

        void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
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
