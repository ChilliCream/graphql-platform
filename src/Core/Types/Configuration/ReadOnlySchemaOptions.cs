using System;

namespace HotChocolate.Configuration
{
    public class ReadOnlySchemaOptions
        : IReadOnlySchemaOptions
    {
        public ReadOnlySchemaOptions(IReadOnlySchemaOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            QueryTypeName = options.QueryTypeName
                ?? "Query";
            MutationTypeName = options.MutationTypeName
                ?? "Mutation";
            SubscriptionTypeName = options.SubscriptionTypeName
                ?? "Subscription";
            StrictValidation = options.StrictValidation;
            UseXmlDocumentation = options.UseXmlDocumentation;
            FieldMiddleware = options.FieldMiddleware;
        }

        public string QueryTypeName { get; }

        public string MutationTypeName { get; }

        public string SubscriptionTypeName { get; }

        public bool StrictValidation { get; }

        public bool UseXmlDocumentation { get; }

        public FieldMiddlewareApplication FieldMiddleware { get; }
    }
}
