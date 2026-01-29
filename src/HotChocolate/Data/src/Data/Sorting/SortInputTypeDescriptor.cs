using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Sorting;

public class SortInputTypeDescriptor
    : DescriptorBase<SortInputTypeConfiguration>
    , ISortInputTypeDescriptor
{
    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Configuration.EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Configuration.RuntimeType = typeof(object);
        Configuration.Name = Convention.GetTypeName(entityType);
        Configuration.Description = Convention.GetTypeDescription(entityType);
        Configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Configuration.Scope = scope;
    }

    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Configuration.RuntimeType = typeof(object);
        Configuration.EntityType = typeof(object);
        Configuration.Scope = scope;
    }

    protected SortInputTypeDescriptor(
        IDescriptorContext context,
        SortInputTypeConfiguration configuration,
        string? scope)
        : base(context)
    {
        Convention = context.GetSortConvention(scope);
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected ISortConvention Convention { get; }

    protected internal override SortInputTypeConfiguration Configuration { get; protected set; } =
        new SortInputTypeConfiguration();

    protected BindableList<SortFieldDescriptor> Fields { get; } =
        [];

    Type IRuntimeTypeProvider.RuntimeType => Configuration.RuntimeType;

    protected override void OnCreateConfiguration(
        SortInputTypeConfiguration configuration)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.ConfigurationsAreApplied)
        {
            DescriptorAttributeHelper.ApplyConfiguration(
                Context,
                this,
                Configuration.EntityType);

            Configuration.ConfigurationsAreApplied = true;
        }

        var fields = new Dictionary<string, SortFieldConfiguration>(StringComparer.Ordinal);
        var handledProperties = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateConfiguration()),
            f => f.Member,
            fields,
            handledProperties);

        OnCompleteFields(fields, handledProperties);

        Configuration.Fields.AddRange(fields.Values);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, SortFieldConfiguration> fields,
        ISet<MemberInfo> handledProperties)
    {
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        Configuration.IsNamed = true;
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Description(
        string? value)
    {
        Configuration.Description = value;
        return this;
    }

    protected ISortInputTypeDescriptor BindFields(
        BindingBehavior bindingBehavior)
    {
        Configuration.Fields.BindingBehavior = bindingBehavior;
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
            Fields.FirstOrDefault(t => t.Configuration.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = SortFieldDescriptor.New(Context, name, Configuration.Scope);
            Fields.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Ignore(string name)
    {
        var fieldDescriptor =
            Fields.FirstOrDefault(t => t.Configuration.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = SortFieldDescriptor.New(
                Context,
                name,
                Configuration.Scope);
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
        Configuration.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        Configuration.AddDirective(new TDirective(), Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public ISortInputTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
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
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static SortInputTypeDescriptor From(
        IDescriptorContext context,
        SortInputTypeConfiguration configuration,
        string? scope = null)
        => new(context, configuration, scope);

    public static SortInputTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        SortInputTypeConfiguration configuration,
        string? scope = null)
        => new(context, configuration, scope);

    public static SortInputTypeDescriptor<T> From<T>(
        SortInputTypeDescriptor descriptor,
        string? scope = null)
        => From<T>(descriptor.Context, descriptor.Configuration, scope);
}
