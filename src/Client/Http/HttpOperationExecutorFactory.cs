using System;
using System.Net.Http;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorFactory
        : IOperationExecutorFactory
    {
        private readonly OperationDelegate<IHttpOperationContext> _executeOperation;
        private readonly Func<HttpClient> _clientFactory;
        private readonly IOperationFormatter _operationFormatter;
        private readonly IResultParserCollection _resultParserResolver;

        public HttpOperationExecutorFactory(
            string name,
            Func<string, HttpClient> clientFactory,
            OperationDelegate<IHttpOperationContext> executeOperation,
            IOperationFormatter operationFormatter,
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
            _operationFormatter = operationFormatter
                ?? throw new ArgumentNullException(nameof(operationFormatter));
            _resultParserResolver = resultParserResolver
                ?? throw new ArgumentNullException(nameof(resultParserResolver));
        }

        public string Name { get; }

        public IOperationExecutor CreateExecutor()
        {
            return new HttpOperationExecutor(
                _clientFactory,
                _executeOperation,
                _operationFormatter,
                _resultParserResolver);
        }
    }
}
