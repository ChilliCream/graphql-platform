using System.Collections.Generic;

namespace HotChocolate.Execution.Configuration
{
    public class RequestExecutorFactoryOptions
    {
        public SchemaBuilder? SchemaBuilder { get; set; }

        public IList<SchemaBuilderAction> SchemaBuilderActions { get; } =
            new List<SchemaBuilderAction>();

        public IList<RequestServicesMiddleware> Pipeline { get; } =
            new List<RequestServicesMiddleware>();
    }
}