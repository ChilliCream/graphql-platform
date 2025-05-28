using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public class FilterOperationFieldDescriptor
    : ArgumentDescriptorBase<FilterOperationFieldConfiguration>
    , IFilterOperationFieldDescriptor
{
    protected FilterOperationFieldDescriptor(
        IDescriptorContext context,
        int operationId,
        string? scope)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);
        Configuration.Id = operationId;
        Configuration.Name = convention.GetOperationName(operationId);
        Configuration.Description = convention.GetOperationDescription(operationId);
        Configuration.Scope = scope;
        Configuration.Flags = CoreFieldFlags.FilterOperationField;
    }

    protected internal new FilterOperationFieldConfiguration Configuration
        => base.Configuration;

    protected override void OnCreateConfiguration(
        FilterOperationFieldConfiguration configuration)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, Property: not null })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Configuration.Property);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateConfiguration(configuration);

        Context.Descriptors.Pop();
    }

    public IFilterOperationFieldDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IFilterOperationFieldDescriptor Ignore(bool ignore = true)
    {
        Configuration.Ignore = ignore;
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
        Configuration.Id = operation;
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

    public InputFieldConfiguration CreateFieldConfiguration() => CreateConfiguration();

    public static FilterOperationFieldDescriptor New(
        IDescriptorContext context,
        int operation,
        string? scope = null) =>
        new(context, operation, scope);
}
