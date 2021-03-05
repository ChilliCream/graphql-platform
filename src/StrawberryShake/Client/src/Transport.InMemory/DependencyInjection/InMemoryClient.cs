using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.InMemory
{
    /// <summary>
    /// Represents a client for sending and receiving messaged to a local schema
    /// </summary>
    public class InMemoryClient : IInMemoryClient
    {
        private readonly string _name;

        /// <summary>
        /// Initializes a new instance of a <see cref="InMemoryClient"/>
        /// </summary>
        /// <param name="name">
        /// The name of the client
        /// </param>
        public InMemoryClient(string name)
        {
            _name = !string.IsNullOrEmpty(name)
                ? name
                : throw ThrowHelper.Argument_IsNullOrEmpty(nameof(name));
        }

        /// <inheritdoc />
        public NameString SchemaName { get; set; } = Schema.DefaultName;

        /// <inheritdoc />
        public IRequestExecutor? Executor { get; set; }

        /// <inheritdoc />
        public IList<IInMemoryRequestInterceptor> RequestInterceptors { get; } =
            new List<IInMemoryRequestInterceptor>();

        /// <inheritdoc />
        public string Name => _name;

        /// <inheritdoc />
        public async ValueTask<IExecutionResult> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (Executor is null)
            {
                throw ThrowHelper.InMemoryClient_NoExecutorConfigured(_name);
            }

            var requestBuilder = new QueryRequestBuilder();

            if (request.Id is null)
            {
                requestBuilder.SetQuery(Utf8GraphQLParser.Parse(request.Document.Body));
            }
            else
            {
                requestBuilder.SetQueryId(request.Id);
            }

            requestBuilder.SetOperation(request.Name);
            requestBuilder.SetVariableValues(request.Variables);
            requestBuilder.SetExtensions(request.GetExtensionsOrNull());
            requestBuilder.SetProperties(request.GetContextDataOrNull());

            IServiceProvider applicationService = Executor.Services.GetApplicationServices();
            foreach (var interceptor in RequestInterceptors)
            {
                await interceptor
                    .OnCreateAsync(applicationService, request, requestBuilder, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await Executor
                .ExecuteAsync(requestBuilder.Create(), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
