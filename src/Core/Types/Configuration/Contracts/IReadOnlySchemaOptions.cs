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
        /// Defines on which fields a middleware pipeline can be applied on.
        /// </summary>
        FieldMiddlewareApplication FieldMiddleware { get; }
    }
}
