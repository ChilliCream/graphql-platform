using System;
using System.Net.Http;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorFactory
        : IOperationExecutorFactory
    {
        private readonly OperationDelegate _executeOperation;
        private readonly Func<HttpClient> _clientFactory;
        private readonly IServiceProvider _services;

        public HttpOperationExecutorFactory(
            string name,
            Func<string, HttpClient> clientFactory,
            OperationDelegate executeOperation,
            IServiceProvider services)
        {
            if (clientFactory is null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            _clientFactory = () => clientFactory(name);
            _executeOperation = executeOperation
                ?? throw new ArgumentNullException(nameof(executeOperation));
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public string Name { get; }

        public IOperationExecutor CreateExecutor()
        {
            return new HttpOperationExecutor(
                _clientFactory,
                _executeOperation,
                _services);
        }
    }
}
