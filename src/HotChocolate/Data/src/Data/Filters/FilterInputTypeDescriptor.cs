using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Filters;

public class FilterInputTypeDescriptor
    : DescriptorBase<FilterInputTypeConfiguration>
    , IFilterInputTypeDescriptor
{
    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        Type entityType,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Configuration.EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
        Configuration.RuntimeType = typeof(object);
        Configuration.Name = Convention.GetTypeName(entityType);
        Configuration.Description = Convention.GetTypeDescription(entityType);
        Configuration.Fields.BindingBehavior = context.Options.DefaultBindingBehavior;
        Configuration.Scope = scope;
        Configuration.UseAnd = Convention.IsAndAllowed();
        Configuration.UseOr = Convention.IsOrAllowed();
    }

    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Configuration.RuntimeType = typeof(object);
        Configuration.EntityType = typeof(object);
        Configuration.Scope = scope;
    }

    protected FilterInputTypeDescriptor(
        IDescriptorContext context,
        FilterInputTypeConfiguration configuration,
        string? scope)
        : base(context)
    {
        Convention = context.GetFilterConvention(scope);
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected IFilterConvention Convention { get; }

    protected internal override FilterInputTypeConfiguration Configuration { get; protected set; } =
        new();

    protected BindableList<FilterFieldDescriptor> Fields { get; } = [];

    protected BindableList<FilterOperationFieldDescriptor> Operations { get; } = [];

    Type IRuntimeTypeProvider.RuntimeType => Configuration.RuntimeType;

    protected override void OnCreateConfiguration(FilterInputTypeConfiguration configuration)
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

        var fields = new Dictionary<string, FilterFieldConfiguration>(StringComparer.Ordinal);
        var handledProperties = new HashSet<MemberInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Fields.Select(t => t.CreateConfiguration())
                .Concat(Operations.Select(t => t.CreateConfiguration())),
            f => f.Member,
            fields,
            handledProperties);

        OnCompleteFields(fields, handledProperties);

        Configuration.Fields.AddRange(fields.Values);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteFields(
        IDictionary<string, FilterFieldConfiguration> fields,
        ISet<MemberInfo> handledProperties)
    {
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        Configuration.IsNamed = true;
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Description(string? value)
    {
        Configuration.Description = value;
        return this;
    }

    protected IFilterInputTypeDescriptor BindFields(BindingBehavior bindingBehavior)
    {
        Configuration.Fields.BindingBehavior = bindingBehavior;
        return this;
    }

    protected IFilterInputTypeDescriptor BindFieldsExplicitly() =>
        BindFields(BindingBehavior.Explicit);

    protected IFilterInputTypeDescriptor BindFieldsImplicitly() =>
        BindFields(BindingBehavior.Implicit);

    /// <inheritdoc />
    public IFilterOperationFieldDescriptor Operation(int operationId)
    {
        var fieldDescriptor =
            Operations.FirstOrDefault(t => t.Configuration.Id == operationId);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterOperationFieldDescriptor.New(
                Context,
                operationId,
                Configuration.Scope);
            Operations.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public IFilterFieldDescriptor Field(string name)
    {
        var fieldDescriptor =
            Fields.FirstOrDefault(t => t.Configuration.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterFieldDescriptor.New(Context, name, Configuration.Scope);
            Fields.Add(fieldDescriptor);
        }

        return fieldDescriptor;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Ignore(int operationId)
    {
        var fieldDescriptor =
            Operations.FirstOrDefault(t => t.Configuration.Id == operationId);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterOperationFieldDescriptor.New(
                Context,
                operationId,
                Configuration.Scope);
            Operations.Add(fieldDescriptor);
        }

        fieldDescriptor.Ignore();
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Ignore(string name)
    {
        var fieldDescriptor =
            Fields.FirstOrDefault(t => t.Configuration.Name == name);

        if (fieldDescriptor is null)
        {
            fieldDescriptor = FilterFieldDescriptor.New(
                Context,
                name,
                Configuration.Scope);
            Fields.Add(fieldDescriptor);
        }

        fieldDescriptor.Ignore();
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor AllowOr(bool allow = true)
    {
        Configuration.UseOr = allow;
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor AllowAnd(bool allow = true)
    {
        Configuration.UseAnd = allow;
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive<TDirective>(TDirective directive)
        where TDirective : class
    {
        Configuration.AddDirective(directive, Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive<TDirective>()
        where TDirective : class, new()
    {
        Configuration.AddDirective(new TDirective(), Context.TypeInspector);
        return this;
    }

    /// <inheritdoc />
    public IFilterInputTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static FilterInputTypeDescriptor New(
        IDescriptorContext context,
        Type entityType,
        string? scope = null) =>
        new FilterInputTypeDescriptor(context, entityType, scope);

    public static FilterInputTypeDescriptor<T> New<T>(
        IDescriptorContext context,
        Type entityType,
        string? scope = null) =>
        new FilterInputTypeDescriptor<T>(context, entityType, scope);

    public static FilterInputTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType,
        string? scope = null)
    {
        var descriptor = New(context, schemaType, scope);
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static FilterInputTypeDescriptor From(
        IDescriptorContext context,
        FilterInputTypeConfiguration configuration,
        string? scope = null)
        => new(context, configuration, scope);

    public static FilterInputTypeDescriptor<T> From<T>(
        IDescriptorContext context,
        FilterInputTypeConfiguration configuration,
        string? scope = null)
        => new(context, configuration, scope);

    public static FilterInputTypeDescriptor<T> From<T>(
        FilterInputTypeDescriptor descriptor,
        string? scope = null)
        => From<T>(descriptor.Context, descriptor.Configuration, scope);
}
