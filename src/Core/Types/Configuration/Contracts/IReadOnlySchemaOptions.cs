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

        BindingBehavior DefaultBindingBehavior { get; }
    }
}
