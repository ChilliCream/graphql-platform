using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration
{
    public class DelegateTypeInitializationInterceptor<T>
        : ITypeInitializationInterceptor
        where T : DefinitionBase
    {
        private readonly Func<ITypeSystemObjectContext, bool> _canHandle;
        private readonly Action<ITypeDiscoveryContext>? _onBeforeInitialize;
        private readonly OnInitializeType<T>? _onAfterInitialize;
        private readonly OnInitializeType<T>? _onBeforeRegisterDependencies;
        private readonly OnInitializeType<T>? _onAfterRegisterDependencies;
        private readonly OnCompleteType<T>? _onBeforeCompleteName;
        private readonly OnCompleteType<T>? _onAfterCompleteName;
        private readonly OnCompleteType<T>? _onBeforeCompleteType;
        private readonly OnCompleteType<T>? _onAfterCompleteType;

        public DelegateTypeInitializationInterceptor(
            Func<ITypeSystemObjectContext, bool>? canHandle = null,
            Action<ITypeDiscoveryContext>? onBeforeInitialize = null,
            OnInitializeType<T>? onAfterInitialize = null,
            OnInitializeType<T>? onBeforeRegisterDependencies = null,
            OnInitializeType<T>? onAfterRegisterDependencies = null,
            OnCompleteType<T>? onBeforeCompleteName = null,
            OnCompleteType<T>? onAfterCompleteName = null,
            OnCompleteType<T>? onBeforeCompleteType = null,
            OnCompleteType<T>? onAfterCompleteType = null)
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

        public void OnBeforeInitialize(ITypeDiscoveryContext discoveryContext)
        {
            _onBeforeInitialize?.Invoke(discoveryContext);
        }

        public void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, DefinitionBase? definition, IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterInitialize?.Invoke(discoveryContext, casted, contextData);
            }
        }

        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeRegisterDependencies?.Invoke(discoveryContext, casted, contextData);
            }
        }

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterRegisterDependencies?.Invoke(discoveryContext, casted, contextData);
            }
        }

        public void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeCompleteName?.Invoke(completionContext, casted, contextData);
            }
        }

        public void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterCompleteName?.Invoke(completionContext, casted, contextData);
            }
        }

        public void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeCompleteType?.Invoke(completionContext, casted, contextData);
            }
        }

        public void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterCompleteType?.Invoke(completionContext, casted, contextData);
            }
        }

        public void OnTypesInitialized(IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            throw new NotImplementedException();
        }

        public void OnTypesCompletedName(IReadOnlyCollection<ITypeCompletionContext> completionContext)
        {
            throw new NotImplementedException();
        }

        public void OnTypesCompleted(IReadOnlyCollection<ITypeCompletionContext> completionContext)
        {
            throw new NotImplementedException();
        }
    }
}