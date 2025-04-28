using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class DirectiveTypeDescriptor
    : DescriptorBase<DirectiveTypeConfiguration>
    , IDirectiveTypeDescriptor
{
    protected internal DirectiveTypeDescriptor(
        IDescriptorContext context,
        Type clrType)
        : base(context)
    {
        if (clrType is null)
        {
            throw new ArgumentNullException(nameof(clrType));
        }

        Configuration.RuntimeType = clrType;
        Configuration.Name = context.Naming.GetTypeName(
            clrType, TypeKind.Directive);
        Configuration.Description = context.Naming.GetTypeDescription(
            clrType, TypeKind.Directive);
        Configuration.IsPublic =
            context.Options.DefaultDirectiveVisibility == DirectiveVisibility.Public;
    }

    protected internal DirectiveTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
    }

    protected internal DirectiveTypeDescriptor(
        IDescriptorContext context,
        DirectiveTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override DirectiveTypeConfiguration Configuration { get; protected set; } = new();

    protected ICollection<DirectiveArgumentDescriptor> Arguments { get; } =
        new List<DirectiveArgumentDescriptor>();

    protected override void OnCreateConfiguration(
        DirectiveTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.AttributesAreApplied && Configuration.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Configuration.RuntimeType);
            Configuration.AttributesAreApplied = true;
        }

        var arguments = new Dictionary<string, DirectiveArgumentConfiguration>(StringComparer.Ordinal);
        var handledMembers = new HashSet<PropertyInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Arguments.Select(t => t.CreateConfiguration()),
            f => f.Property,
            arguments,
            handledMembers);

        OnCompleteArguments(arguments, handledMembers);

        definition.Arguments.AddRange(arguments.Values);

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteArguments(
        IDictionary<string, DirectiveArgumentConfiguration> arguments,
        ISet<PropertyInfo> handledProperties)
    {
    }

    public IDirectiveTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IDirectiveTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public IDirectiveArgumentDescriptor Argument(string name)
    {
        var descriptor = Arguments.FirstOrDefault(t => t.Configuration.Name.EqualsOrdinal(name));

        if (descriptor is not null)
        {
            return descriptor;
        }

        descriptor = DirectiveArgumentDescriptor.New(Context, name);
        Arguments.Add(descriptor);
        return descriptor;
    }

    public IDirectiveTypeDescriptor Location(DirectiveLocation value)
    {
        Configuration.Locations |= value;
        return this;
    }

    public IDirectiveTypeDescriptor Use(DirectiveMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        Configuration.MiddlewareComponents.Add(middleware);
        return this;
    }

    public IDirectiveTypeDescriptor Use<TMiddleware>()
        where TMiddleware : class
    {
        return Use(DirectiveClassMiddlewareFactory.Create<TMiddleware>());
    }

    public IDirectiveTypeDescriptor Use<TMiddleware>(
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        return Use(DirectiveClassMiddlewareFactory.Create(factory));
    }

    public IDirectiveTypeDescriptor Repeatable()
    {
        Configuration.IsRepeatable = true;
        return this;
    }

    public IDirectiveTypeDescriptor Public()
    {
        Configuration.IsPublic = true;
        return this;
    }

    public IDirectiveTypeDescriptor Internal()
    {
        Configuration.IsPublic = false;
        return this;
    }

    public static DirectiveTypeDescriptor New(IDescriptorContext context, Type clrType)
        => new(context, clrType);

    public static DirectiveTypeDescriptor New(IDescriptorContext context) => new(context);

    public static DirectiveTypeDescriptor<T> New<T>(IDescriptorContext context) => new(context);

    public static DirectiveTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType)
    {
        var descriptor = New(context, schemaType);
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static DirectiveTypeDescriptor From(
        IDescriptorContext context,
        DirectiveTypeConfiguration definition)
        => new(context, definition);

    public static DirectiveTypeDescriptor From<T>(
        IDescriptorContext context,
        DirectiveTypeConfiguration definition)
        => new DirectiveTypeDescriptor<T>(context, definition);
}
