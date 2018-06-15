using System;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        private int _maxExecutionDepth = 8;
        private TimeSpan _executionTimeout = TimeSpan.FromSeconds(5);

        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public int MaxExecutionDepth { get; set; }

        public TimeSpan ExecutionTimeout { get; set; }

        public IServiceProvider Services { get; set; }

        public bool StrictValidation { get; set; }
    }
}
