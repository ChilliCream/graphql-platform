using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterOperationFieldDescriptor
    : ArgumentDescriptorBase<FilterOperationFieldDefinition>
    , IFilterOperationFieldDescriptor
{
    protected FilterOperationFieldDescriptor(
        IDescriptorContext context,
        int operationId,
        string? scope)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);
        Definition.Id = operationId;
        Definition.Name = convention.GetOperationName(operationId);
        Definition.Description = convention.GetOperationDescription(operationId);
        Definition.Scope = scope;
        Definition.Flags = FieldFlags.FilterOperationField;
    }

    protected internal new FilterOperationFieldDefinition Definition => base.Definition;

    protected override void OnCreateDefinition(
        FilterOperationFieldDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, Property: not null })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.Property);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    public IFilterOperationFieldDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IFilterOperationFieldDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
        return this;
    }

    public new IFilterOperationFieldDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new IFilterOperationFieldDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    public new IFilterOperationFieldDescriptor Type<TInputType>(
        TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    public new IFilterOperationFieldDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    public new IFilterOperationFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    public IFilterOperationFieldDescriptor Operation(int operation)
    {
        Definition.Id = operation;
        return this;
    }

    public new IFilterOperationFieldDescriptor DefaultValue(
        IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new IFilterOperationFieldDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new IFilterOperationFieldDescriptor Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new IFilterOperationFieldDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new IFilterOperationFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public InputFieldDefinition CreateFieldDefinition() => CreateDefinition();

    public static FilterOperationFieldDescriptor New(
        IDescriptorContext context,
        int operation,
        string? scope = null) =>
        new(context, operation, scope);
}
