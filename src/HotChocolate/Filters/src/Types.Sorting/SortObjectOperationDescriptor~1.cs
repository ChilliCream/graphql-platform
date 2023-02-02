using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Sorting;

[Obsolete("Use HotChocolate.Data.")]
public class SortObjectOperationDescriptor<TObject>
    : SortObjectOperationDescriptor
    , ISortObjectOperationDescriptor<TObject>
{
    protected SortObjectOperationDescriptor(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        : base(context, name, type, operation)
    {
    }

    public new ISortObjectOperationDescriptor<TObject> Name(string value)
    {
        base.Name(value);
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Ignore(bool ignore = true)
    {
        base.Ignore(ignore);
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Directive<T>(T directiveInstance)
        where T : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Directive<T>()
        where T : class, new()
    {
        base.Directive<T>();
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public ISortObjectOperationDescriptor<TObject> Type(
        Action<ISortInputTypeDescriptor<TObject>> descriptor)
    {
        var type = new SortInputType<TObject>(descriptor);
        base.Type(type);
        return this;
    }

    public new ISortObjectOperationDescriptor<TObject> Type<TFilter>()
        where TFilter : SortInputType<TObject>
    {
        base.Type<TFilter>();
        return this;
    }

    public new static SortObjectOperationDescriptor<TObject> New(
        IDescriptorContext context,
        string name,
        TypeReference type,
        SortOperation operation)
        => new(context, name, type, operation);

    public static new SortObjectOperationDescriptor<TObject> CreateOperation(
        PropertyInfo property,
        IDescriptorContext context)
    {
        var operation = new SortOperation(property, true);
        var name = context.Naming.GetMemberName(property, MemberKind.InputObjectField);
        var typeReference = context.TypeInspector.GetTypeRef(
            typeof(SortInputType<>).MakeGenericType(typeof(TObject)),
            TypeContext.Input);

        return New(context, name, typeReference, operation);
    }
}
