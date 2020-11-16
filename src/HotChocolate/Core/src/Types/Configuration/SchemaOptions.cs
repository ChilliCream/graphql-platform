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

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        public bool SortFieldsByName { get; set; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        public bool RemoveUnreachableTypes { get; set; } = false;

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        public BindingBehavior DefaultBindingBehavior { get; set; } =
            BindingBehavior.Implicit;

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        public FieldMiddlewareApplication FieldMiddleware { get; set; } =
            FieldMiddlewareApplication.UserDefinedFields;

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
