using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class SubscriptionQuery
    {
        public string OperationName { get; set; }
        public string Query { get; set; }
        public Dictionary<string, object> Variables { get; set; }
    }
}
