#if !ASPNETCLASSIC

using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class DictionaryOperationMessage
        : OperationMessage
    {
        public IReadOnlyDictionary<string, object> Payload { get; set; }
    }
}

#endif
