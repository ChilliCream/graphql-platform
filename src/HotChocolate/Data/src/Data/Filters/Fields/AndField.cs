using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public sealed class AndField
        : InputField
        , IAndField
    {
        internal AndField(
            IDescriptorContext context,
            string? scope,
            InputObjectType filterType)
            : base(CreateDefinition(context, filterType, scope))
        {
        }

        private static InputFieldDefinition CreateDefinition(
            IDescriptorContext context,
            InputObjectType filterType,
            string? scope)
        {
            FilterOperationFieldDefinition? definition = FilterOperationFieldDescriptor
                .New(context, DefaultOperations.And, scope)
                .CreateDefinition();

            definition.Type = new SchemaTypeReference(
                new ListType(new NonNullType(filterType)),
                scope: scope);

            return definition;
        }
    }
}
