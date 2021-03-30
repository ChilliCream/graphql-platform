using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public sealed class AndField
        : InputField
        , IAndField
    {
        internal AndField(
            IDescriptorContext context,
            string? scope)
            : base(CreateDefinition(context, scope), default)
        {
        }

        public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

        IFilterInputType IAndField.DeclaringType => DeclaringType;

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

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context,
            string? scope) =>
            FilterOperationFieldDescriptor
                .New(context, DefaultFilterOperations.And, scope)
                .CreateDefinition();
    }
}
