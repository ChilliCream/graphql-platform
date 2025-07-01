using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public class FilterFieldDescriptor
    : ArgumentDescriptorBase<FilterFieldConfiguration>
    , IFilterFieldDescriptor
{
    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        string fieldName)
        : base(context)
    {
        Configuration.Name = fieldName;
        Configuration.Scope = scope;
    }

    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        MemberInfo member)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);

        Configuration.Member = member ?? throw new ArgumentNullException(nameof(member));

        Configuration.Name = convention.GetFieldName(member);
        Configuration.Description = convention.GetFieldDescription(member);
        Configuration.Type = convention.GetFieldType(member);
        Configuration.Scope = scope;
    }

    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        Expression expression)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);

        Configuration.Expression = expression;
        Configuration.Scope = scope;

        if (Configuration.Expression is LambdaExpression lambda)
        {
            Configuration.Type = convention.GetFieldType(lambda.ReturnType);
            Configuration.RuntimeType = lambda.ReturnType;
        }
    }

    protected internal FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Configuration.Scope = scope;
    }

    protected internal new FilterFieldConfiguration Configuration
    {
        get => base.Configuration;
        protected set => base.Configuration = value;
    }

    internal InputFieldConfiguration CreateFieldConfiguration() => CreateConfiguration();

    protected override void OnCreateConfiguration(
        FilterFieldConfiguration configuration)
    {
        Context.Descriptors.Push(this);

        if (Configuration is { AttributesAreApplied: false, Member: not null })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Configuration.Member);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateConfiguration(configuration);

        Context.Descriptors.Pop();
    }

    public IFilterFieldDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IFilterFieldDescriptor Ignore(bool ignore = true)
    {
        Configuration.Ignore = ignore;
        return this;
    }

    public new IFilterFieldDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new IFilterFieldDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    public new IFilterFieldDescriptor Type<TInputType>(
        TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    public new IFilterFieldDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    public new IFilterFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    public new IFilterFieldDescriptor DefaultValue(
        IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new IFilterFieldDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new IFilterFieldDescriptor Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new IFilterFieldDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new IFilterFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static FilterFieldDescriptor New(
        IDescriptorContext context,
        string? scope,
        MemberInfo member)
        => new(context, scope, member);

    public static FilterFieldDescriptor New(
        IDescriptorContext context,
        string fieldName,
        string? scope)
        => new(context, scope, fieldName);

    internal static FilterFieldDescriptor New(
        IDescriptorContext context,
        string? scope,
        Expression expression)
        => new(context, scope, expression);
}
