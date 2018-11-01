using Microsoft.AspNetCore.Http;

namespace HotChocolate
{
    public class GraphiQLOptions
    {
        public PathString Route { get; set; } = "/ui";

        public PathString QueryRoute { get; set; }

        public PathString SubscriptionRoute { get; set; } = "/ws";
    }
}
