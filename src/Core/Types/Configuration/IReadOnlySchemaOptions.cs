namespace HotChocolate.Configuration
{
    public interface IReadOnlySchemaOptions
    {
        string QueryTypeName { get; }
        string MutationTypeName { get; }
        string SubscriptionTypeName { get; }
        bool StrictValidation { get; }
    }
}
