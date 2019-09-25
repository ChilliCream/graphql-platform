using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public class SchemaOptions
        : ISchemaOptions
    {
        public string QueryTypeName { get; set; }

        public string MutationTypeName { get; set; }

        public string SubscriptionTypeName { get; set; }

        public bool StrictValidation { get; set; } = true;

        public bool UseXmlDocumentation { get; set; } = true;

        public BindingBehavior DefaultBindingBehavior { get; set; } =
            BindingBehavior.Implicit;

        public FieldMiddlewareApplication FieldMiddleware
        {
            get;
            set;
        } = FieldMiddlewareApplication.UserDefinedFields;

        public static SchemaOptions FromOptions(IReadOnlySchemaOptions options)
        {
            return new SchemaOptions
            {
                QueryTypeName = options.QueryTypeName,
                MutationTypeName = options.MutationTypeName,
                SubscriptionTypeName = options.SubscriptionTypeName,
                StrictValidation = options.StrictValidation,
                UseXmlDocumentation = options.UseXmlDocumentation,
                FieldMiddleware = options.FieldMiddleware,
                DefaultBindingBehavior = options.DefaultBindingBehavior
            };
        }
    }
}
