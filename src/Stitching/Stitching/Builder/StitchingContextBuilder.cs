
using System;
using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate.Stitching
{
    public class StitchingContextBuilder
    {
        private readonly Dictionary<string, IQueryExecutor> _executors =
            new Dictionary<string, IQueryExecutor>();

        public StitchingContextBuilder AddExecutor(
            string schemaName,
            IQueryExecutor executor)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            _executors.Add(schemaName, executor);
            return this;
        }

        public StitchingContextBuilder AddExecutor(
            Func<RemoteExecutorBuilder, RemoteExecutorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddExecutor(configure(RemoteExecutorBuilder.New()));
        }

        public StitchingContextBuilder AddExecutor(
            RemoteExecutorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            IRemoteExecutorAccessor accessor = builder.Build();
            _executors.Add(accessor.SchemaName, accessor.Executor);
            return this;
        }

        public IStitchingContext Build()
        {
            if (_executors.Count == 0)
            {
                throw new InvalidOperationException(
                    "Register query executors first.");
            }

            return new StitchingContext(_executors);
        }

        public static StitchingContextBuilder New() =>
            new StitchingContextBuilder();
    }
}
