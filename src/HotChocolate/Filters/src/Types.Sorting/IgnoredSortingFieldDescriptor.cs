using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Types.TypeContext;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
internal class IgnoredSortingFieldDescriptor
    : SortOperationDescriptorBase
{
    protected IgnoredSortingFieldDescriptor(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        : base(context, name, type, operation)
    {

        Definition.Ignore = true;
    }

    public static IgnoredSortingFieldDescriptor New(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        => new(context, name, type, operation);

    public static IgnoredSortingFieldDescriptor CreateOperation(
        PropertyInfo property,
        IDescriptorContext context)
    {
        var operation = new SortOperation(property);
        var typeReference = context.TypeInspector.GetTypeRef(typeof(SortOperationKindType), Input);
        var name = context.Naming.GetMemberName(property, MemberKind.InputObjectField);

        return New(context, name, typeReference, operation);
    }
}
