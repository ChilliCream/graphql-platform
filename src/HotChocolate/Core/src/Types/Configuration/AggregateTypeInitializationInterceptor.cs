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
            ITypeDiscoveryContext context)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnBeforeInitialize(context);
                }
            }
        }

        public void OnAfterInitialize(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnAfterInitialize(
                        context, definition, contextData);
                }
            }
        }

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnAfterRegisterDependencies(
                        context, definition, contextData);
                }
            }
        }

        public void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnBeforeRegisterDependencies(
                        context, definition, contextData);
                }
            }
        }

        public void OnBeforeCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnBeforeCompleteName(context, definition, contextData);
                }
            }
        }

        public void OnAfterCompleteName(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnAfterCompleteName(context, definition, contextData);
                }
            }
        }

        public void OnBeforeCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnBeforeCompleteType(context, definition, contextData);
                }
            }
        }

        public void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _initInterceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnAfterCompleteType(context, definition, contextData);
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
