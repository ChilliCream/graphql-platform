using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class SortOperationDescriptor
    : SortOperationDescriptorBase
    , ISortOperationDescriptor
{
    protected SortOperationDescriptor(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        : base(context, name, type, operation)
    {
    }

    protected internal sealed override SortOperationDefintion Definition { get; protected set; } =
        new();

    protected override void OnCreateDefinition(
        SortOperationDefintion definition)
    {
        if (!Definition.AttributesAreApplied && Definition.Operation?.Property is not null)
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.Operation.Property);
            Definition.AttributesAreApplied = true;
        }
        base.OnCreateDefinition(definition);
    }

    public ISortOperationDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
        return this;
    }

    public new ISortOperationDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public new ISortOperationDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new ISortOperationDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new ISortOperationDescriptor Directive<T>()
        where T : class, new()
    {
        base.Directive<T>();
        return this;
    }

    public new ISortOperationDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static SortOperationDescriptor New(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        => new(context, name, type, operation);

    public static SortOperationDescriptor CreateOperation(
        PropertyInfo property,
        IDescriptorContext context)
    {
        var operation = new SortOperation(property);

        var typeReference = context.TypeInspector.GetTypeRef(
            typeof(SortOperationKindType),
            TypeContext.Input);

        var name = context.Naming.GetMemberName(property, MemberKind.InputObjectField);

        return New(context, name, typeReference, operation);
    }
}
