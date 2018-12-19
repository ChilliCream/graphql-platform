using System;

namespace HotChocolate.Configuration
{
    public interface IReadOnlySchemaOptions
    {
        string QueryTypeName { get; }
        string MutationTypeName { get; }
        string SubscriptionTypeName { get; }
        int? MaxExecutionDepth { get; }
        TimeSpan ExecutionTimeout { get; }
        bool StrictValidation { get; }
        bool DeveloperMode { get; }
    }
}
