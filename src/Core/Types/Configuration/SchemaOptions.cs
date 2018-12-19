using System;
using System.Diagnostics;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        private const int _defaultMaxExecutionTimeout = 30;
        private const int _defaultMaxDevExecutionTimeout = 360;

        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public int? MaxExecutionDepth { get; set; }

        public TimeSpan ExecutionTimeout { get; set; } =
            TimeSpan.FromSeconds(Debugger.IsAttached
                ? _defaultMaxDevExecutionTimeout
                : _defaultMaxExecutionTimeout);

        public IServiceProvider Services { get; set; }

        public bool StrictValidation { get; set; } = true;

        public bool DeveloperMode { get; set; } = Debugger.IsAttached;
    }
}
