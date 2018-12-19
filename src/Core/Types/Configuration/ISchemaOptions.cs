using System;

namespace HotChocolate.Configuration
{
    public interface ISchemaOptions
        : IReadOnlySchemaOptions
    {
        new string QueryTypeName { get; set; }
        new string MutationTypeName { get; set; }
        new string SubscriptionTypeName { get; set; }
        new int? MaxExecutionDepth { get; set; }
        new TimeSpan ExecutionTimeout { get; set; }
        new bool StrictValidation { get; set; }
        new bool DeveloperMode { get; set; }
    }
}
