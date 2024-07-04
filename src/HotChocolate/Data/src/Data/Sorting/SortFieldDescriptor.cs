using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting;

public class SortFieldDescriptor
    : ArgumentDescriptorBase<SortFieldDefinition>
    , ISortFieldDescriptor
{
    protected SortFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        string fieldName)
        : base(context)
    {
        Definition.Name = fieldName;
        Definition.Scope = scope;
        Definition.Flags = FieldFlags.SortOperationField;
    }

    protected SortFieldDescriptor(
         IDescriptorContext context,
         string? scope,
         Expression expression)
         : base(context)
    {
        var convention = context.GetSortConvention(scope);

        Definition.Expression = expression;
        Definition.Scope = scope;
        Definition.Flags = FieldFlags.SortOperationField;
        if (Definition.Expression is LambdaExpression lambda)
        {
            Definition.Type = convention.GetFieldType(lambda.ReturnType);
            Definition.RuntimeType = lambda.ReturnType;
        }
    }

    protected SortFieldDescriptor(
        IDescriptorContext context,
        string? scope,
        MemberInfo member)
        : base(context)
    {
        var convention = context.GetSortConvention(scope);

        Definition.Member = member ??
            throw new ArgumentNullException(nameof(member));

        Definition.Name = convention.GetFieldName(member);
        Definition.Description = convention.GetFieldDescription(member);
        Definition.Type = convention.GetFieldType(member);
        Definition.Scope = scope;
        Definition.Flags = FieldFlags.SortOperationField;
    }

    protected internal SortFieldDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Definition.Scope = scope;
        Definition.Flags = FieldFlags.SortOperationField;
    }

    protected internal new SortFieldDefinition Definition
    {
        get => base.Definition;
        protected set => base.Definition = value;
    }

    internal InputFieldDefinition CreateFieldDefinition() => CreateDefinition();

    protected override void OnCreateDefinition(
        SortFieldDefinition definition)
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

    public ISortFieldDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public ISortFieldDescriptor Ignore(bool ignore = true)
    {
        Definition.Ignore = ignore;
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
