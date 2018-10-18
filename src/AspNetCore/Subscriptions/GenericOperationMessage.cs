using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class GenericOperationMessage
        : OperationMessage
    {
        public JObject Payload { get; set; }
    }
}
