using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal sealed class AggregateTypeInitializationInterceptor
        : ITypeInitializationInterceptor
    {
        private readonly IReadOnlyCollection<ITypeInitializationInterceptor> _interceptors;

        public AggregateTypeInitializationInterceptor()
        {
            _interceptors = Array.Empty<ITypeInitializationInterceptor>();
        }

        public AggregateTypeInitializationInterceptor(
            IReadOnlyCollection<ITypeInitializationInterceptor> interceptors)
        {
            _interceptors = interceptors
                ?? throw new ArgumentNullException(nameof(interceptors));
        }

        public bool CanHandle(ITypeSystemObjectContext context) => true;

        public void OnAfterRegisterDependencies(
            ITypeDiscoveryContext context,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
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
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
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
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
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
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
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
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
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
            foreach (ITypeInitializationInterceptor interceptor in _interceptors)
            {
                if (interceptor.CanHandle(context))
                {
                    interceptor.OnAfterCompleteType(context, definition, contextData);
                }
            }
        }
    }
}
