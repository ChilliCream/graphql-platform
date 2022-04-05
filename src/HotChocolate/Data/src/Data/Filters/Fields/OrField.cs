using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public sealed class OrField
    : FilterOperationField
    , IOrField
{
    internal OrField(IDescriptorContext context, int index, string? scope)
        : base(CreateDefinition(context, scope), index)
    {
    }

    public new FilterInputType DeclaringType => (FilterInputType)base.DeclaringType;

    IFilterInputType IOrField.DeclaringType => DeclaringType;

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldDefinition definition)
    {
        definition.Type = TypeReference.Parse(
            $"[{context.Type.Name}!]",
            TypeContext.Input,
            context.Type.Scope);

        base.OnCompleteField(context, declaringMember, definition);
    }

    private static FilterOperationFieldDefinition CreateDefinition(
        IDescriptorContext context,
        string? scope) =>
        FilterOperationFieldDescriptor
            .New(context, DefaultFilterOperations.Or, scope)
            .CreateDefinition();
}
