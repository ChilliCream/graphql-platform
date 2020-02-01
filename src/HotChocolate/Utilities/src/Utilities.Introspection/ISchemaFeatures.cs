namespace HotChocolate.Utilities.Introspection
{
    public interface ISchemaFeatures
    {
        bool HasDirectiveLocations { get; }
        bool HasRepeatableDirectives { get; }
        bool HasSubscriptionSupport { get; }
    }
}
