using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Represents mutable schema options.
    /// </summary>
    public class SchemaOptions : ISchemaOptions
    {
        /// <summary>
        /// Gets or sets the name of the query type.
        /// </summary>
        public string QueryTypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the mutation type.
        /// </summary>
        public string MutationTypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription type.
        /// </summary>
        public string SubscriptionTypeName { get; set; }

        /// <summary>
        /// Defines if the schema allows the query type to be omitted.
        /// </summary>
        public bool StrictValidation { get; set; } = true;

        /// <summary>
        /// Defines if the CSharp XML documentation shall be integrated.
        /// </summary>
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
            return new()
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
