using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    public class TypeInterceptor
        : ITypeInitializationInterceptor
    {
        public virtual bool TriggerAggregations => false;

        public virtual bool CanHandle(ITypeSystemObjectContext context) => true;

        public virtual void OnBeforeInitialize(
            ITypeDiscoveryContext discoveryContext)
        {
        }

        public virtual void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }
        
        public virtual void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
        }

        public virtual void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> completionContext)
        {
        }

        public virtual void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> completionContext)
        {
        }
    }
}
