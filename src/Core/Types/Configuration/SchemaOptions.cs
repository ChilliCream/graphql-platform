using System;
using System.Diagnostics;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public IServiceProvider Services { get; set; }

        public bool StrictValidation { get; set; } = true;

        public bool DeveloperMode { get; set; } = Debugger.IsAttached;
    }
}
