using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public sealed class AndField
        : InputField
        , IAndField
    {
        internal AndField(
            IDescriptorContext context)
            : base(CreateDefinition(context))
        {
        }

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context) =>
            InputFieldDescriptor
                .New(context, "AND")
                .CreateDefinition();

        protected override void OnCompleteField(
            ITypeCompletionContext context,
            InputFieldDefinition definition)
        {
            definition.Type = TypeReference.Create(
                new ListTypeNode(
                    new NonNullTypeNode(
                        new NamedTypeNode(context.Type.Name))),
                TypeContext.Input,
                context.Type.Scope);

            base.OnCompleteField(context, definition);
        }
    }
}
