using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    public class TypeInitializationInterceptor
        : ITypeInitializationInterceptor
    {
        public virtual bool CanHandle(ITypeSystemObjectContext context) => true;

        public virtual void OnBeforeInitialize(
            ITypeDiscoveryContext context)
        {
        }

        public virtual void OnAfterInitialize(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }
    }
}
