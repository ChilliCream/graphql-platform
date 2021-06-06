using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Configuration
{
    public class RequestExecutorSetup
    {
        private readonly List<SchemaBuilderAction> _schemaBuilderActions = new();
        private readonly List<RequestExecutorOptionsAction> _requestExecutorOptionsActions = new();
        private readonly List<RequestCoreMiddleware> _pipeline = new();
        private readonly List<Action<IServiceCollection>> _schemaServices = new();
        private readonly List<OnRequestExecutorCreatedAction> _onRequestExecutorCreated = new();
        private readonly List<OnRequestExecutorEvictedAction> _onRequestExecutorEvicted = new();

        public ISchema? Schema { get; set; }

        public ISchemaBuilder? SchemaBuilder { get; set; }

        public RequestExecutorOptions? RequestExecutorOptions { get; set; }

        public IList<SchemaBuilderAction> SchemaBuilderActions =>
            _schemaBuilderActions;

        public IList<RequestExecutorOptionsAction> RequestExecutorOptionsActions =>
            _requestExecutorOptionsActions;

        public IList<RequestCoreMiddleware> Pipeline =>
            _pipeline;

        public IList<Action<IServiceCollection>> SchemaServices =>
            _schemaServices;

        public IList<OnRequestExecutorCreatedAction> OnRequestExecutorCreated =>
            _onRequestExecutorCreated;

        public IList<OnRequestExecutorEvictedAction> OnRequestExecutorEvicted =>
            _onRequestExecutorEvicted;

        public void CopyTo(RequestExecutorSetup options)
        {
            options.Schema = Schema;
            options.SchemaBuilder = SchemaBuilder;
            options.RequestExecutorOptions = RequestExecutorOptions;
            options._schemaBuilderActions.AddRange(_schemaBuilderActions);
            options._requestExecutorOptionsActions.AddRange(_requestExecutorOptionsActions);
            options._pipeline.AddRange(_pipeline);
            options._schemaServices.AddRange(_schemaServices);
            options._onRequestExecutorCreated.AddRange(_onRequestExecutorCreated);
            options._onRequestExecutorEvicted.AddRange(_onRequestExecutorEvicted);
        }
    }
}
