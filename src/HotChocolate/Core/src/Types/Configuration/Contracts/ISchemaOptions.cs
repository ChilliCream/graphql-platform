using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface ISchemaOptions
        : IReadOnlySchemaOptions
    {
        new string QueryTypeName { get; set; }

        new string MutationTypeName { get; set; }

        new string SubscriptionTypeName { get; set; }

        new bool StrictValidation { get; set; }

        new bool UseXmlDocumentation { get; set; }

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        bool SortFieldsByName { get; set; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        new bool RemoveUnreachableTypes { get; set; }

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        new BindingBehavior DefaultBindingBehavior { get; set; }

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        new FieldMiddlewareApplication FieldMiddleware { get; set; }
    }
}
