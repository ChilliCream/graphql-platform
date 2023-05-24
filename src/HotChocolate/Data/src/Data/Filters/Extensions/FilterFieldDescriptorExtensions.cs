using System;
using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types;

public static class FilterFieldDescriptorExtensions
{
    public static void MakeNullable(this IFilterFieldDescriptor descriptor) =>
        descriptor.Extend()
            .OnBeforeCreate(
                (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));

    public static void MakeNullable(this IFilterOperationFieldDescriptor descriptor) =>
        descriptor.Extend()
            .OnBeforeCreate(
                (c, def) => def.Type = RewriteTypeToNullableType(def, c.TypeInspector));


    private static TypeReference RewriteTypeToNullableType(
        FilterFieldDefinition definition,
        ITypeInspector typeInspector)
    {
        var reference = definition.Type;

        if (reference is null)
            throw new InvalidOperationException("Type reference is null.");

        return reference.GetNullableAnalogue(typeInspector);
    }
}
