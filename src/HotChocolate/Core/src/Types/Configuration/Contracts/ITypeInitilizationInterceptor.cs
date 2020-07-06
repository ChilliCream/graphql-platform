using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeInitializationInterceptor
    {
        bool CanHandle(ITypeSystemObjectContext context);

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
}
