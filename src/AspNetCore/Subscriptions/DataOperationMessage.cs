using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class DataOperationMessage
        : OperationMessage
    {
        public IReadOnlyDictionary<string, object> Payload { get; set; }
    }
}
