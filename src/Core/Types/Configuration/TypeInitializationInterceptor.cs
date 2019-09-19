using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    public class TypeInitializationInterceptor
        : ITypeInitializationInterceptor
    {
        public virtual bool CanHandle(ITypeSystemObjectContext context) => true;

        public virtual void OnAfterRegisterDependencies(
            IInitializationContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeRegisterDependencies(
            IInitializationContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeCompleteName(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteName(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnBeforeCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }

        public virtual void OnAfterCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
        }
    }
}
