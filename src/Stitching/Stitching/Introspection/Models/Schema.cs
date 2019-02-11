using System.Collections.Generic;

namespace HotChocolate.Stitching.Introspection.Models
{
    internal class Schema
    {
        public RootTypeRef QueryType { get; set; }
        public RootTypeRef MutationType { get; set; }
        public RootTypeRef SubscriptionType { get; set; }
        public ICollection<FullType> Types { get; set; }
        public ICollection<Directive> Directives { get; set; }
    }
}
