using System;
using System.ComponentModel.Design;

namespace HotChocolate.Execution.Configuration
{
    public sealed class NamedRequestExecutorFactoryOptions
        : INamedRequestExecutorFactoryOptions
    {
        private readonly Action<RequestExecutorFactoryOptions> _configure;

        public NamedRequestExecutorFactoryOptions(
            NameString schemaName,
            Action<RequestExecutorFactoryOptions> configure)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public NamedRequestExecutorFactoryOptions(
            NameString schemaName,
            RequestExecutorFactoryOptions options)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _configure = options.CopyTo;
        }

        public NameString SchemaName { get; }

        public void Configure(RequestExecutorFactoryOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _configure(options);
        }
    }
}
