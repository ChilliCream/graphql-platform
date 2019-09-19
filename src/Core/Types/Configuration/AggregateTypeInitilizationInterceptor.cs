using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    internal sealed class AggregateTypeInitilizationInterceptor
        : ITypeInitializationInterceptor
    {
        private readonly IReadOnlyCollection<ITypeInitializationInterceptor> _interceptors;

        public AggregateTypeInitilizationInterceptor()
        {
            _interceptors = Array.Empty<ITypeInitializationInterceptor>();
        }

        public AggregateTypeInitilizationInterceptor(
            IReadOnlyCollection<ITypeInitializationInterceptor> interceptors)
        {
            _interceptors = interceptors
                ?? throw new ArgumentNullException(nameof(interceptors));
        }

        public bool CanHandle(ITypeSystemObjectContext context) => true;

        public void OnAfterRegisterDependencies(
            IInitializationContext context,
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
            IInitializationContext context,
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
            ICompletionContext context,
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
            ICompletionContext context,
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
            ICompletionContext context,
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
            ICompletionContext context,
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
