namespace HotChocolate.AspNetCore
{
    public class MiddlewareOptions
    {
        public int QueryCacheSize { get; set; } = 100;
        public string Route { get; set; }
        public string PostRoute { get; set; }
        public string GetRoute { get; set; }
        public string SubscriptionRoute { get; set; }
    }
}
