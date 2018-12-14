#if !ASPNETCLASSIC

using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class DataOperationMessage
        : OperationMessage
    {
        public IReadOnlyDictionary<string, object> Payload { get; set; }
    }
}

#endif
