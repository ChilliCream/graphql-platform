using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public class TypeInterceptor
        : ITypeInterceptor
        , ITypeInitializationFlowInterceptor
        , ITypeRegistryInterceptor
    {
        public virtual bool TriggerAggregations => false;

        public virtual bool CanHandle(ITypeSystemObjectContext context) => true;

        public virtual void OnBeforeDiscoverTypes()
        {
        }

        public virtual void OnAfterDiscoverTypes()
        {
        }

        public virtual void OnBeforeInitialize(
            ITypeDiscoveryContext discoveryContext)
        {
        }

        public virtual void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnTypeRegistered(
            ITypeDiscoveryContext discoveryContext)
        {
        }

        public virtual void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
        }

        public virtual void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnBeforeCompleteTypeNames()
        {
        }

        public virtual void OnAfterCompleteTypeNames()
        {
        }

        public virtual void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
        }

        public virtual void OnBeforeMergeTypeExtensions()
        {
        }

        public virtual void OnAfterMergeTypeExtensions()
        {
        }

        public virtual void OnBeforeCompleteTypes()
        {
        }

        public virtual void OnAfterCompleteTypes()
        {
        }

        public virtual void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
        }

        public virtual void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
        }

        public virtual bool TryCreateScope(
            ITypeDiscoveryContext discoveryContext,
            [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? typeDependencies)
        {
            typeDependencies = null;
            return false;
        }
    }
}
