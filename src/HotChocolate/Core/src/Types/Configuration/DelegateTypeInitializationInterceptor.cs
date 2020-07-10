using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
{
    public class DelegateTypeInitializationInterceptor
        : ITypeInitializationInterceptor
    {
        private readonly Func<ITypeSystemObjectContext, bool> _canHandle;
        private readonly Action<ITypeDiscoveryContext>? _onBeforeInitialize;
        private readonly OnInitializeType? _onAfterInitialize;
        private readonly OnInitializeType? _onBeforeRegisterDependencies;
        private readonly OnInitializeType? _onAfterRegisterDependencies;
        private readonly OnCompleteType? _onBeforeCompleteName;
        private readonly OnCompleteType? _onAfterCompleteName;
        private readonly OnCompleteType? _onBeforeCompleteType;
        private readonly OnCompleteType? _onAfterCompleteType;

        public DelegateTypeInitializationInterceptor(
            Func<ITypeSystemObjectContext, bool>? canHandle = null,
            Action<ITypeDiscoveryContext>? onBeforeInitialize = null,
            OnInitializeType? onAfterInitialize = null,
            OnInitializeType? onBeforeRegisterDependencies = null,
            OnInitializeType? onAfterRegisterDependencies = null,
            OnCompleteType? onBeforeCompleteName = null,
            OnCompleteType? onAfterCompleteName = null,
            OnCompleteType? onBeforeCompleteType = null,
            OnCompleteType? onAfterCompleteType = null)
        {
            _canHandle = canHandle ?? (c => true);
            _onBeforeInitialize = onBeforeInitialize;
            _onAfterInitialize = onAfterInitialize;
            _onBeforeRegisterDependencies = onBeforeRegisterDependencies;
            _onAfterRegisterDependencies = onAfterRegisterDependencies;
            _onBeforeCompleteName = onBeforeCompleteName;
            _onAfterCompleteName = onAfterCompleteName;
            _onBeforeCompleteType = onBeforeCompleteType;
            _onAfterCompleteType = onAfterCompleteType;
        }

        public bool CanHandle(ITypeSystemObjectContext context) =>
            _canHandle(context);

        public void OnBeforeInitialize(ITypeDiscoveryContext context) =>
            _onBeforeInitialize?.Invoke(context);

        public void OnAfterInitialize(
            ITypeDiscoveryContext context, 
            DefinitionBase? definition, 
            IDictionary<string, object?> contextData) =>
            _onAfterInitialize?.Invoke(context, definition, contextData);
        

        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeRegisterDependencies?.Invoke(context, definition, contextData);

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterRegisterDependencies?.Invoke(context, definition, contextData);

        public void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeCompleteName?.Invoke(context, definition, contextData);

        public void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterCompleteName?.Invoke(context, definition, contextData);

        public void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeCompleteType?.Invoke(context, definition, contextData);

        public void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterCompleteType?.Invoke(context, definition, contextData);
    }
}