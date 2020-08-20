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
            string? scope,
            InputObjectType filterType)
            : base(CreateDefinition(context, filterType, scope))
        {
        }

        public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

        IFilterInputType IOrField.DeclaringType => DeclaringType;

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context,
            InputObjectType filterType,
            string? scope)
        {
            FilterOperationFieldDefinition? definition = FilterOperationFieldDescriptor
                .New(context, DefaultOperations.Or, scope)
                .CreateDefinition();

            definition.Type = new SchemaTypeReference(
                new ListType(new NonNullType(filterType)),
                scope: scope);

            return definition;
        }
    }
}
