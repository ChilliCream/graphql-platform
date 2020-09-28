using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public class ReadOnlySchemaOptions
        : IReadOnlySchemaOptions
    {
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
        }

        public string QueryTypeName { get; }

        public string MutationTypeName { get; }

        public string SubscriptionTypeName { get; }

        public bool StrictValidation { get; }

        public bool UseXmlDocumentation { get; }

        public bool SortFieldsByName { get; }

        public bool RemoveUnreachableTypes { get; }

        public BindingBehavior DefaultBindingBehavior { get; }

        public FieldMiddlewareApplication FieldMiddleware { get; }
    }
}
