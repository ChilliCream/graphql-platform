using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// This is an input type descriptor for lists. This is sloley used for the inline customization
/// for filtering.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListOperationTypeDescriptor<T> : IListOperationTypeDescriptor<T>
{
    private readonly IFilterInputTypeDescriptor _descriptor;

    public ListOperationTypeDescriptor(IFilterInputTypeDescriptor descriptor)
    {
        _descriptor = descriptor;
    }

    public Type RuntimeType => _descriptor.RuntimeType;

    public IFilterInputTypeDescriptor AllowAnd(bool allow = true)
        => _descriptor.AllowAnd(allow);

    public IFilterInputTypeDescriptor AllowOr(bool allow = true)
        => _descriptor.AllowOr(allow);

    public IFilterInputTypeDescriptor Description(string? value)
        => _descriptor.Description(value);

    public IFilterInputTypeDescriptor Directive<TDirective>(TDirective directive)
        where TDirective : class
        => _descriptor.Directive(directive);

    public IFilterInputTypeDescriptor Directive<TDirective>()
        where TDirective : class, new()
        => _descriptor.Directive<TDirective>();

    public IFilterInputTypeDescriptor Directive(NameString name, params ArgumentNode[] arguments)
        => _descriptor.Directive(name, arguments);

    public IDescriptorExtension<FilterInputTypeDefinition> Extend()
        => _descriptor.Extend();

    public IFilterFieldDescriptor Field(NameString name)
        => _descriptor.Field(name);

    public IFilterFieldDescriptor Field(NameString name, Action<IFilterInputTypeDescriptor> configure)
        => _descriptor.Field(name, configure);

    public IFilterInputTypeDescriptor Ignore(int operationId)
        => _descriptor.Ignore(operationId);

    public IFilterInputTypeDescriptor Ignore(NameString name)
        => _descriptor.Ignore(name);

    public IFilterInputTypeDescriptor Name(NameString value)
        => _descriptor.Name(value);

    public IFilterOperationFieldDescriptor Operation(int operationId)
        => _descriptor.Operation(operationId);
}
