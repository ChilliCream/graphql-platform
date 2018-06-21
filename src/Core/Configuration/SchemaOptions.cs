using System;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public int MaxExecutionDepth { get; set; } = 8;

        public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public IServiceProvider Services { get; set; }

        public bool StrictValidation { get; set; } = true;

        public bool DeveloperMode { get; set; } = false;
    }
}
