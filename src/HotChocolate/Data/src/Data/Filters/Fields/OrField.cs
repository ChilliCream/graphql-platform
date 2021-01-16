using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public sealed class OrField
        : InputField
        , IOrField
    {
        internal OrField(
            IDescriptorContext context,
            string? scope)
            : base(CreateDefinition(context, scope))
        {
        }

        public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

        IFilterInputType IOrField.DeclaringType => DeclaringType;

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
                .New(context, DefaultFilterOperations.Or, scope)
                .CreateDefinition();
    }
}
