#if !ASPNETCLASSIC

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class OperationMessage
    {
        public string Id { get; set; }

        public string Type { get; set; }
    }
}

#endif
