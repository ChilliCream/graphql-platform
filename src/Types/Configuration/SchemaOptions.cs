using System;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        private const int _defaultMaxExecutionDepth = 8;
        private const int _defaultMaxExecutionTimeout = 30;

        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public int MaxExecutionDepth { get; set; } =
            _defaultMaxExecutionDepth;

        public TimeSpan ExecutionTimeout { get; set; } =
            TimeSpan.FromSeconds(_defaultMaxExecutionTimeout);

        public IServiceProvider Services { get; set; }

        public bool StrictValidation { get; set; } = true;

        public bool DeveloperMode { get; set; } = false;
    }
}
