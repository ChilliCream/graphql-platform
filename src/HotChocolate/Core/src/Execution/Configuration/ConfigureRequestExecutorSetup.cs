using System;
using System.ComponentModel.Design;

namespace HotChocolate.Execution.Configuration
{
    public sealed class ConfigureRequestExecutorSetup
        : IConfigureRequestExecutorSetup
    {
        private readonly Action<RequestExecutorSetup> _configure;

        public ConfigureRequestExecutorSetup(
            NameString schemaName,
            Action<RequestExecutorSetup> configure)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public ConfigureRequestExecutorSetup(
            NameString schemaName,
            RequestExecutorSetup options)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _configure = options.CopyTo;
        }

        public NameString SchemaName { get; }

        public void Configure(RequestExecutorSetup options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _configure(options);
        }
    }
}
