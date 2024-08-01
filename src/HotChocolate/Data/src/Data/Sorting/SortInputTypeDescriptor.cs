using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Sorting;

public class SortInputTypeDescriptor
    : DescriptorBase<SortInputTypeDefinition>
    , ISortInputTypeDescriptor
{
    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Definition.EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Definition.RuntimeType = typeof(object);
        Definition.Name = Convention.GetTypeName(entityType);
        Definition.Description = Convention.GetTypeDescription(entityType);
        Definition.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Definition.Scope = scope;
    }

    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Definition.RuntimeType = typeof(object);
        Definition.EntityType = typeof(object);
        Definition.Scope = scope;
    }

    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        SortInputTypeDefinition definition,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected ISortConvention Convention { get; }

    protected internal override SortInputTypeDefinition Definition { get; protected set; } =
        new SortInputTypeDefinition();

    protected BindableList<SortFieldDescriptor> Fields { get; } =
        [];

    Type IHasRuntimeType.RuntimeType => Definition.RuntimeType;

    protected override void OnCreateDefinition(
        SortInputTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (Definition is { AttributesAreApplied: false, EntityType: not null, })
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Definition.EntityType);
            Definition.AttributesAreApplied = true;
        }

        var fields = new Dictionary<string, SortFieldDefinition>(StringComparer.Ordinal);
        var handledProperties = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateDefinition()),
            f => f.Member,
            fields,
            handledProperties);

        OnCompleteFields(fields, handledProperties);

        Definition.Fields.AddRange(fields.Values);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, SortFieldDefinition> fields,
        ISet<MemberInfo> handledProperties)
    {
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        Definition.IsNamed = true;
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Description(
        string? value)
    {
        Definition.Description = value;
        return this;
    }

    protected ISortInputTypeDescriptor BindFields(
        BindingBehavior bindingBehavior)
    {
        Definition.Fields.BindingBehavior = bindingBehavior;
        return this;
    }

    protected ISortInputTypeDescriptor BindFieldsExplicitly()
        => BindFields(BindingBehavior.Explicit);

    protected ISortInputTypeDescriptor BindFieldsImplicitly()
        => BindFields(BindingBehavior.Implicit);

    /// <inheritdoc />
    public ISortFieldDescriptor Field(string name)
    {
        var fieldDescriptor =
            Fields.FirstOrDefault(t => t.Definition.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = SortFieldDescriptor.New(Context, name, Definition.Scope);
            Fields.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Ignore(string name)
    {
        var fieldDescriptor =
            Fields.FirstOrDefault(t => t.Definition.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = SortFieldDescriptor.New(
                Context,
                name,
                Definition.Scope);
            Fields.Add(fieldDescriptor);
        }

        fieldDescriptor.Ignore();
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Directive<TDirective>(
        TDirective directive)
        where TDirective : class
    {
        Definition.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        Definition.AddDirective(new TDirective(), Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Definition.AddDirective(name, arguments);
        return this;
    }

    public static SortInputTypeDescriptor New(
        IDescriptorContext context,
        Type entityType,
        string? scope = null)
        => new(context, entityType, scope);

    public static SortInputTypeDescriptor<T> New<T>(
        IDescriptorContext context,
        Type entityType,
        string? scope = null)
        => new(context, entityType, scope);

    public static SortInputTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType,
        string? scope = null)
    {
        var descriptor = New(context, schemaType, scope);
        descriptor.Definition.RuntimeType = typeof(object);
        return descriptor;
    }

    public static SortInputTypeDescriptor From(
        IDescriptorContext context,
        SortInputTypeDefinition definition,
        string? scope = null)
        => new(context, definition, scope);

    public static SortInputTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        SortInputTypeDefinition definition,
        string? scope = null)
        => new(context, definition, scope);

    public static SortInputTypeDescriptor<T> From<T>(
        SortInputTypeDescriptor descriptor,
        string? scope = null)
        => From<T>(descriptor.Context, descriptor.Definition, scope);
}
