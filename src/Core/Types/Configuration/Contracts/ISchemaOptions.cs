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

        new BindingBehavior DefaultBindingBehavior { get; set; }
    }
}
