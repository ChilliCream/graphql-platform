using System;
using System.Net.Http;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorFactory
        : IOperationExecutorFactory
    {
        private readonly HttpOperationDelegate _executeOperation;
        private readonly Func<HttpClient> _clientFactory;
        private readonly IResultParserCollection _resultParserResolver;

        public HttpOperationExecutorFactory(
            string name,
            Func<string, HttpClient> clientFactory,
            HttpOperationDelegate executeOperation,
            IResultParserCollection resultParserResolver)
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
            _resultParserResolver = resultParserResolver
                ?? throw new ArgumentNullException(nameof(resultParserResolver));
        }

        public string Name { get; }

        public IOperationExecutor CreateExecutor()
        {
            return new HttpOperationExecutor(
                _clientFactory,
                _executeOperation,
                _resultParserResolver);
        }
    }
}
