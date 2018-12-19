#if !ASPNETCLASSIC

using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class GenericOperationMessage
        : OperationMessage
    {
        public JObject Payload { get; set; }
    }
}

#endif
