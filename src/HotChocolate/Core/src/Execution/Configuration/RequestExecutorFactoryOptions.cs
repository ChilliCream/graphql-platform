using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Options;

namespace HotChocolate.Execution.Configuration
{
    public class RequestExecutorFactoryOptions
    {
        public ISchema? Schema { get; set; }

        public ISchemaBuilder? SchemaBuilder { get; set; }

        public RequestExecutorOptions? RequestExecutorOptions { get; set; }

        public IList<SchemaBuilderAction> SchemaBuilderActions { get; } =
            new List<SchemaBuilderAction>();

        public IList<RequestExecutorOptionsAction> RequestExecutorOptionsActions { get; } =
            new List<RequestExecutorOptionsAction>();

        public IList<RequestCoreMiddleware> Pipeline { get; } =
            new List<RequestCoreMiddleware>();

        public IList<Action<IServiceCollection>> SchemaServices { get; } =
            new List<Action<IServiceCollection>>();

        public IList<OnRequestExecutorCreatedAction> OnRequestExecutorCreated { get; } =
            new List<OnRequestExecutorCreatedAction>();

        public IList<OnRequestExecutorEvictedAction> OnRequestExecutorEvicted { get; } =
            new List<OnRequestExecutorEvictedAction>();
    }
}
