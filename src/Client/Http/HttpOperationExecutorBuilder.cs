using System.Collections.Generic;
using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;
using System.Linq;
using StrawberryShake.Http.Utilities;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorBuilder
        : IHttpOperationExecutorBuilder
    {
        private readonly Dictionary<Type, List<object>> _services =
            new Dictionary<Type, List<object>>();
        private readonly List<IServiceProvider> _serviceProviders =
            new List<IServiceProvider>();
        private Func<IServiceProvider, Func<HttpClient>>? _clientFactory;
        private Func<IServiceProvider, OperationDelegate>? _pipelineFactory;

        public IHttpOperationExecutorBuilder AddService(
            Type serviceType, object serviceInstance)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (serviceInstance is null)
            {
                throw new ArgumentNullException(nameof(serviceInstance));
            }

            if (!_services.TryGetValue(serviceType, out List<object>? services))
            {
                services = new List<object>();
                _services.Add(serviceType, services);
            }
            services.Add(serviceInstance);
            return this;
        }

        public IHttpOperationExecutorBuilder AddServices(
            IServiceProvider services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _serviceProviders.Add(services);
            return this;
        }

        public IHttpOperationExecutorBuilder SetClient(
            Func<IServiceProvider, Func<HttpClient>> clientFactory)
        {
            if (clientFactory is null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            _clientFactory = clientFactory;
            return this;
        }

        public IHttpOperationExecutorBuilder SetPipeline(
            Func<IServiceProvider, OperationDelegate> pipelineFactory)
        {
            if (pipelineFactory is null)
            {
                throw new ArgumentNullException(nameof(pipelineFactory));
            }

            _pipelineFactory = pipelineFactory;
            return this;
        }

        public IOperationExecutor Build()
        {
            if (_clientFactory is null)
            {
                throw new InvalidOperationException(
                    "The HTTP client must be provided in order to " +
                    "create an executor.");
            }

            if (_pipelineFactory is null)
            {
                throw new InvalidOperationException(
                    "There must be pipeline defined in order to execute " +
                    "client requests.");
            }

            IServiceProvider services = BuildServices();
            Func<HttpClient> clientFactory = _clientFactory(services);
            OperationDelegate pipeline = _pipelineFactory(services);

            return new HttpOperationExecutor(clientFactory, pipeline, services);
        }

        private IServiceProvider BuildServices()
        {
            IServiceProvider? services = null;

            while (_serviceProviders.Count > 0)
            {
                if (services == null)
                {
                    services = _serviceProviders.Last();
                }
                else
                {
                    services = new CombinedServiceProvider(
                        _serviceProviders.Last(),
                        services);
                }
                _serviceProviders.RemoveAt(_serviceProviders.Count - 1);
            }

            if (_services.Count > 0)
            {
                var localServices = new DictionaryServiceProvider(_services);
                return services is null
                    ? (IServiceProvider)localServices
                    : new CombinedServiceProvider(localServices, services);
            }

            if (services is null)
            {
                throw new InvalidOperationException(
                    "There was no service provider or service specified.");
            }

            return services;
        }

        public static HttpOperationExecutorBuilder New() =>
            new HttpOperationExecutorBuilder();
    }
}
