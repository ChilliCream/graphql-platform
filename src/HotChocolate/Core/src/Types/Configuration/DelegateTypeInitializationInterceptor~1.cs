using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
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

        public bool CanHandle(ITypeSystemObjectContext context) =>
            _canHandle(context);

        public void OnBeforeInitialize(ITypeDiscoveryContext context)
        {
            _onBeforeInitialize?.Invoke(context);
        }

        public void OnAfterInitialize(ITypeDiscoveryContext context, DefinitionBase? definition, IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterInitialize?.Invoke(context, casted, contextData);
            }
        }

        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeRegisterDependencies?.Invoke(context, casted, contextData);
            }
        }

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterRegisterDependencies?.Invoke(context, casted, contextData);
            }
        }

        public void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeCompleteName?.Invoke(context, casted, contextData);
            }
        }

        public void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterCompleteName?.Invoke(context, casted, contextData);
            }
        }

        public void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onBeforeCompleteType?.Invoke(context, casted, contextData);
            }
        }

        public void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is T casted)
            {
                _onAfterCompleteType?.Invoke(context, casted, contextData);
            }
        }
    }
}