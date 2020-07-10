using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeInitializationInterceptor
    {
        bool TriggerAggregations { get; }

        bool CanHandle(ITypeSystemObjectContext context);

        void OnBeforeInitialize(
            ITypeDiscoveryContext discoveryContext);

        void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts);

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

        void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> completionContext);

        void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData);

        void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> completionContext);
    }
}
