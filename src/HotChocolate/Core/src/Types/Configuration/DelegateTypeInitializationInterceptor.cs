using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public class DelegateTypeInterceptor
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

        public DelegateTypeInterceptor(
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

        public bool TriggerAggregations => false;

        public bool CanHandle(ITypeSystemObjectContext context) =>
            _canHandle(context);

        public void OnBeforeInitialize(ITypeDiscoveryContext discoveryContext) =>
            _onBeforeInitialize?.Invoke(discoveryContext);

        public void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterInitialize?.Invoke(discoveryContext, definition, contextData);


        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeRegisterDependencies?.Invoke(discoveryContext, definition, contextData);

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterRegisterDependencies?.Invoke(discoveryContext, definition, contextData);

        public void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeCompleteName?.Invoke(completionContext, definition, contextData);

        public void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterCompleteName?.Invoke(completionContext, definition, contextData);

        public void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onBeforeCompleteType?.Invoke(completionContext, definition, contextData);

        public void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData) =>
            _onAfterCompleteType?.Invoke(completionContext, definition, contextData);

        public void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
        }

        public void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> discoveryContexts)
        {
        }

        public void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> discoveryContexts)
        {
        }
    }
}
