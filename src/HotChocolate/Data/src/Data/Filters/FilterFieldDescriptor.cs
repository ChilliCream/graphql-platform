using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterFieldDescriptor
    : ArgumentDescriptorBase<FilterFieldDefinition>
    , IFilterFieldDescriptor
{
    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        string fieldName)
        : base(context)
    {
        Definition.Name = fieldName;
        Definition.Scope = scope;
    }

    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        MemberInfo member)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);

        Definition.Member = member ?? throw new ArgumentNullException(nameof(member));

        Definition.Name = convention.GetFieldName(member);
        Definition.Description = convention.GetFieldDescription(member);
        Definition.Type = convention.GetFieldType(member);
        Definition.Scope = scope;
    }

    protected FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        Expression expression)
        : base(context)
    {
        var convention = context.GetFilterConvention(scope);

        Definition.Expression = expression;
        Definition.Scope = scope;

        if (Definition.Expression is LambdaExpression lambda)
        {
            Definition.Type = convention.GetFieldType(lambda.ReturnType);
            Definition.RuntimeType = lambda.ReturnType;
        }
    }

    protected internal FilterFieldDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Definition.Scope = scope;
    }

    protected internal new FilterFieldDefinition Definition
    {
        get => base.Definition;
        protected set => base.Definition = value;
    }

    internal InputFieldDefinition CreateFieldDefinition() => CreateDefinition();

    protected override void OnCreateDefinition(
        FilterFieldDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, Member: not null, })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.Member);
            Definition.AttributesAreApplied = true;
        }

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    public IFilterFieldDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IFilterFieldDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
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
