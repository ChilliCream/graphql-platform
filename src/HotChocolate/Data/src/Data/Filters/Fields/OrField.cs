using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public sealed class OrField
    : FilterOperationField
    , IOrField
{
    internal OrField(IDescriptorContext context, int index, string? scope)
        : base(CreateConfiguration(context, scope), index)
    {
    }

    public new FilterInputType DeclaringType => base.DeclaringType;

    IFilterInputType IOrField.DeclaringType => DeclaringType;

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        InputFieldConfiguration definition)
    {
        definition.Type = TypeReference.Parse(
            $"[{context.Type.Name}!]",
            TypeContext.Input,
            context.Type.Scope);

        base.OnCompleteField(context, declaringMember, definition);
    }

    private static FilterOperationFieldConfiguration CreateConfiguration(
        IDescriptorContext context,
        string? scope) =>
        FilterOperationFieldDescriptor
            .New(context, DefaultFilterOperations.Or, scope)
            .CreateConfiguration();
}
