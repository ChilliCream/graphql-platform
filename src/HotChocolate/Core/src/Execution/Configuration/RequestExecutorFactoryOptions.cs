using System.Collections.Generic;

namespace HotChocolate.Execution.Configuration
{
    public class RequestExecutorFactoryOptions
    {
        public IList<SchemaBuilderAction> SchemaBuilderActions { get; } =
            new List<SchemaBuilderAction>();

        public IList<RequestMiddleware> Pipeline { get; } =
            new List<RequestMiddleware>();
    }
}