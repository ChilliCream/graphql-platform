using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class SortFieldDescriptor
    : ArgumentDescriptorBase<SortFieldConfiguration>
    , ISortFieldDescriptor
{
    protected SortFieldDescriptor(IDescriptorContext context, string? scope, string fieldName)
        : base(context)
    {
        Configuration.Name = fieldName;
        Configuration.Scope = scope;
        Configuration.Flags = CoreFieldFlags.SortOperationField;
    }

    protected SortFieldDescriptor(IDescriptorContext context, string? scope, Expression expression)
        : base(context)
    {
        Configuration.Expression = expression;
        Configuration.Scope = scope;
        Configuration.Flags = CoreFieldFlags.SortOperationField;

        if (Configuration.Expression is LambdaExpression lambda)
        {
            var convention = context.GetSortConvention(scope);
            Configuration.Type = convention.GetFieldType(lambda.ReturnType);
            Configuration.RuntimeType = lambda.ReturnType;
        }
    }

    protected SortFieldDescriptor(IDescriptorContext context, string? scope, MemberInfo member)
        : base(context)
    {
        Configuration.Member = member ??
            throw new ArgumentNullException(nameof(member));

        var convention = context.GetSortConvention(scope);
        Configuration.Name = convention.GetFieldName(member);
        Configuration.Description = convention.GetFieldDescription(member);
        Configuration.Type = convention.GetFieldType(member);
        Configuration.Scope = scope;
        Configuration.Flags = CoreFieldFlags.SortOperationField;
    }

    protected internal SortFieldDescriptor(IDescriptorContext context, string? scope)
        : base(context)
    {
        Configuration.Scope = scope;
        Configuration.Flags = CoreFieldFlags.SortOperationField;
    }

    protected internal new SortFieldConfiguration Configuration
    {
        get => base.Configuration;
        protected set => base.Configuration = value;
    }

    protected override void OnCreateConfiguration(
        SortFieldConfiguration configuration)
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

    public ISortFieldDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public ISortFieldDescriptor Ignore(bool ignore = true)
    {
        Configuration.Ignore = ignore;
        return this;
    }

    public new ISortFieldDescriptor Description(string value)
    {
        base.Description(value);
        return this;
    }

    public new ISortFieldDescriptor Type<TInputType>()
        where TInputType : IInputType
    {
        base.Type<TInputType>();
        return this;
    }

    public new ISortFieldDescriptor Type<TInputType>(
        TInputType inputType)
        where TInputType : class, IInputType
    {
        base.Type(inputType);
        return this;
    }

    public new ISortFieldDescriptor Type(ITypeNode typeNode)
    {
        base.Type(typeNode);
        return this;
    }

    public new ISortFieldDescriptor Type(Type type)
    {
        base.Type(type);
        return this;
    }

    public new ISortFieldDescriptor DefaultValue(
        IValueNode value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new ISortFieldDescriptor DefaultValue(object value)
    {
        base.DefaultValue(value);
        return this;
    }

    public new ISortFieldDescriptor Directive<TDirective>(
        TDirective directiveInstance)
        where TDirective : class
    {
        base.Directive(directiveInstance);
        return this;
    }

    public new ISortFieldDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        base.Directive<TDirective>();
        return this;
    }

    public new ISortFieldDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        base.Directive(name, arguments);
        return this;
    }

    public static SortFieldDescriptor New(
        IDescriptorContext context,
        string? scope,
        MemberInfo member)
        => new(context, scope, member);

    public static SortFieldDescriptor New(
        IDescriptorContext context,
        string fieldName,
        string? scope)
        => new(context, scope, fieldName);

    internal static SortFieldDescriptor New(
        IDescriptorContext context,
        string? scope,
        Expression expression)
        => new(context, scope, expression);
}
