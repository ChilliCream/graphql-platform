using System;
using System.Collections.Generic;
using System.Text;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    public interface ITypeInitializationInterceptor
    {
        bool CanHandle(ITypeSystemObjectContext context);

        void OnBeforeRegisterDependencies(
            IInitializationContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);

        void OnAfterRegisterDependencies(
            IInitializationContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);

        void OnBeforeCompleteName(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);

        void OnAfterCompleteName(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);

        void OnBeforeCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);

        void OnAfterCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData);
    }
}
