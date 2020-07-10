using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal sealed class AggregateTypeInitializationInterceptor
        : ITypeInterceptor
    {
        private readonly IReadOnlyCollection<ITypeInitializationInterceptor> _initInterceptors;
        private readonly IReadOnlyCollection<ITypeScopeInterceptor> _scopeInterceptors;

        public AggregateTypeInitializationInterceptor()
        {
            _initInterceptors = Array.Empty<ITypeInitializationInterceptor>();
            _scopeInterceptors = Array.Empty<ITypeScopeInterceptor>();
        }

        public AggregateTypeInitializationInterceptor(
            IReadOnlyCollection<object> interceptors)
        {
            _initInterceptors = interceptors.OfType<ITypeInitializationInterceptor>().ToList();
            _scopeInterceptors = interceptors.OfType<ITypeScopeInterceptor>().ToList();

        }

        public bool CanHandle(ITypeSystemObjectContext context) => true;

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
    }
}
