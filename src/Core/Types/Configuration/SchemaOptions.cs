namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public bool StrictValidation { get; set; } = true;

        public static SchemaOptions FromOptions(IReadOnlySchemaOptions options)
        {
            return new SchemaOptions
            {
                QueryTypeName = options.QueryTypeName,
                MutationTypeName = options.MutationTypeName,
                SubscriptionTypeName = options.SubscriptionTypeName,
                StrictValidation = options.StrictValidation
            };
        }
    }
}
