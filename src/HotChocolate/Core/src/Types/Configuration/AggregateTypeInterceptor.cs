using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal sealed class AggregateTypeInterceptor
        : ITypeInterceptor
        , ITypeInitializationFlowInterceptor
    {
        private readonly IReadOnlyCollection<ITypeInitializationInterceptor> _initInterceptors;
        private readonly IReadOnlyCollection<ITypeInitializationInterceptor> _agrInterceptors;
        private readonly IReadOnlyCollection<ITypeScopeInterceptor> _scopeInterceptors;
        private readonly IReadOnlyCollection<ITypeInitializationFlowInterceptor> _flowInterceptors;

        public AggregateTypeInterceptor()
        {
            _initInterceptors = Array.Empty<ITypeInitializationInterceptor>();
            _agrInterceptors = Array.Empty<ITypeInitializationInterceptor>();
            _scopeInterceptors = Array.Empty<ITypeScopeInterceptor>();
            _flowInterceptors = Array.Empty<ITypeInitializationFlowInterceptor>();
            TriggerAggregations = false;
        }

        public AggregateTypeInterceptor(object interceptor)
            : this(new[] { interceptor })
        {
        }

        public AggregateTypeInterceptor(IReadOnlyCollection<object> interceptors)
        {
            _initInterceptors = interceptors.OfType<ITypeInitializationInterceptor>().ToList();
            _agrInterceptors = _initInterceptors.Where(t => t.TriggerAggregations).ToList();
            _scopeInterceptors = interceptors.OfType<ITypeScopeInterceptor>().ToList();
            _flowInterceptors = interceptors.OfType<ITypeInitializationFlowInterceptor>().ToList();
            TriggerAggregations = _agrInterceptors.Count > 0;
        }

        public bool TriggerAggregations { get; }

        public bool CanHandle(ITypeSystemObjectContext context) => true;

        public void OnBeforeDiscoverTypes()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnBeforeDiscoverTypes();
            }
        }

        public void OnAfterDiscoverTypes()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnAfterDiscoverTypes();
            }
        }

        public void OnBeforeInitialize(
            ITypeDiscoveryContext discoveryContext)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(discoveryContext))
                {
                    interceptor.OnBeforeInitialize(discoveryContext);
                }
            }
        }

        public void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(discoveryContext))
                {
                    interceptor.OnAfterInitialize(
                        discoveryContext, definition, contextData);
                }
            }
        }

        public void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            if (_agrInterceptors.Count > 0)
            {
                var list = new List<ITypeDiscoveryContext>();

                foreach (ITypeInitializationInterceptor interceptor in _agrInterceptors)
                {
                    list.Clear();

                    foreach (ITypeDiscoveryContext discoveryContext in discoveryContexts)
                    {
                        if (interceptor.CanHandle(discoveryContext))
                        {
                            list.Add(discoveryContext);
                        }
                    }

                    interceptor.OnTypesInitialized(list);
                }
            }
        }

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(discoveryContext))
                {
                    interceptor.OnAfterRegisterDependencies(
                        discoveryContext, definition, contextData);
                }
            }
        }

        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(discoveryContext))
                {
                    interceptor.OnBeforeRegisterDependencies(
                        discoveryContext, definition, contextData);
                }
            }
        }

        public void OnBeforeCompleteTypeNames()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnBeforeCompleteTypeNames();
            }
        }

        public void OnAfterCompleteTypeNames()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnAfterCompleteTypeNames();
            }
        }

        public void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(completionContext))
                {
                    interceptor.OnBeforeCompleteName(completionContext, definition, contextData);
                }
            }
        }

        public void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(completionContext))
                {
                    interceptor.OnAfterCompleteName(completionContext, definition, contextData);
                }
            }
        }

        public void OnTypesCompletedName(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
            if (_agrInterceptors.Count > 0)
            {
                var list = new List<ITypeCompletionContext>();

                foreach (ITypeInitializationInterceptor interceptor in _agrInterceptors)
                {
                    list.Clear();

                    foreach (ITypeCompletionContext completionContext in completionContexts)
                    {
                        if (interceptor.CanHandle(completionContext))
                        {
                            list.Add(completionContext);
                        }
                    }

                    interceptor.OnTypesCompletedName(list);
                }
            }
        }

        public void OnBeforeMergeTypeExtensions()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnBeforeMergeTypeExtensions();
            }
        }

        public void OnAfterMergeTypeExtensions()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnAfterMergeTypeExtensions();
            }
        }

        public void OnBeforeCompleteTypes()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnBeforeCompleteTypes();
            }
        }

        public void OnAfterCompleteTypes()
        {
            foreach (ITypeInitializationFlowInterceptor interceptor in _flowInterceptors)
            {
                interceptor.OnAfterCompleteTypes();
            }
        }

        public void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(completionContext))
                {
                    interceptor.OnBeforeCompleteType(completionContext, definition, contextData);
                }
            }
        }

        public void OnAfterCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(completionContext))
                {
                    interceptor.OnAfterCompleteType(completionContext, definition, contextData);
                }
            }
        }

        public bool TryCreateScope(
            ITypeDiscoveryContext discoveryContext,
            [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies)
        {
            foreach (ITypeScopeInterceptor interceptor in _scopeInterceptors)
            {
                if (interceptor.TryCreateScope(discoveryContext, out typeDependencies))
                {
                    return true;
                }
            }

            typeDependencies = null;
            return false;
        }

        public void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
            if (_agrInterceptors.Count > 0)
            {
                var list = new List<ITypeCompletionContext>();

                foreach (ITypeInitializationInterceptor interceptor in _agrInterceptors)
                {
                    list.Clear();

                    foreach (ITypeCompletionContext completionContext in completionContexts)
                    {
                        if (interceptor.CanHandle(completionContext))
                        {
                            list.Add(completionContext);
                        }
                    }

                    interceptor.OnTypesCompleted(list);
                }
            }
        }
    }
}
