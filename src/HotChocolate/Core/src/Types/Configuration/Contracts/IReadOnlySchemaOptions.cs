using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public interface IReadOnlySchemaOptions
    {
        string QueryTypeName { get; }

        string MutationTypeName { get; }

        string SubscriptionTypeName { get; }

        bool StrictValidation { get; }

        bool UseXmlDocumentation { get; }

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        bool SortFieldsByName { get; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        bool RemoveUnreachableTypes { get; }

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        BindingBehavior DefaultBindingBehavior { get; }

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        FieldMiddlewareApplication FieldMiddleware { get; }
    }
}
