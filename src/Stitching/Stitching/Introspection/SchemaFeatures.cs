namespace HotChocolate.Stitching.Introspection
{
    internal sealed class SchemaFeatures
    {
        public bool HasDirectiveLocations { get; set; }

        public bool HasRepeatableDirectives { get; set; }

        public bool HasSubscriptionSupport { get; set; }
    }
}
