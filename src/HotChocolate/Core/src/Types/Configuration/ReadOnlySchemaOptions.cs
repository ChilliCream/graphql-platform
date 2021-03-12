using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// Represents read-only schema options.
    /// </summary>
    public class ReadOnlySchemaOptions : IReadOnlySchemaOptions
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ReadOnlySchemaOptions"/>.
        /// </summary>
        /// <param name="options">
        /// The options that shall be wrapped as read-only options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public ReadOnlySchemaOptions(IReadOnlySchemaOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryTypeName = options.QueryTypeName ?? "Query";
            MutationTypeName = options.MutationTypeName ?? "Mutation";
            SubscriptionTypeName = options.SubscriptionTypeName ?? "Subscription";
            StrictValidation = options.StrictValidation;
            SortFieldsByName = options.SortFieldsByName;
            UseXmlDocumentation = options.UseXmlDocumentation;
            RemoveUnreachableTypes = options.RemoveUnreachableTypes;
            DefaultBindingBehavior = options.DefaultBindingBehavior;
            FieldMiddleware = options.FieldMiddleware;
            PreserveSyntaxNodes = options.PreserveSyntaxNodes;
        }

        /// <summary>
        /// Gets the name of the query type.
        /// </summary>
        public string QueryTypeName { get; }

        /// <summary>
        /// Gets or sets the name of the mutation type.
        /// </summary>
        public string MutationTypeName { get; }

        /// <summary>
        /// Gets or sets the name of the subscription type.
        /// </summary>
        public string SubscriptionTypeName { get; }

        /// <summary>
        /// Defines if the schema allows the query type to be omitted.
        /// </summary>"
        public bool StrictValidation { get; }

        /// <summary>
        /// Defines if the CSharp XML documentation shall be integrated.
        /// </summary>
        public bool UseXmlDocumentation { get; }

        /// <summary>
        /// Defines if fields shall be sorted by name.
        /// Default: <c>false</c>
        /// </summary>
        public bool SortFieldsByName { get; }

        /// <summary>
        /// Defines if syntax nodes shall be preserved on the type system objects
        /// </summary>
        public bool PreserveSyntaxNodes { get; }

        /// <summary>
        /// Defines if types shall be removed from the schema that are
        /// unreachable from the root types.
        /// </summary>
        public bool RemoveUnreachableTypes { get; }

        /// <summary>
        /// Defines the default binding behavior.
        /// </summary>
        public BindingBehavior DefaultBindingBehavior { get; }

        /// <summary>
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        public FieldMiddlewareApplication FieldMiddleware { get; }
    }
}
