using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public class DirectiveTypeDescriptor
    : DescriptorBase<DirectiveTypeDefinition>
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

        Definition.RuntimeType = clrType;
        Definition.Name = context.Naming.GetTypeName(
            clrType, TypeKind.Directive);
        Definition.Description = context.Naming.GetTypeDescription(
            clrType, TypeKind.Directive);
        Definition.IsPublic =
            context.Options.DefaultDirectiveVisibility == DirectiveVisibility.Public;
    }

    protected internal DirectiveTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Definition.RuntimeType = typeof(object);
    }

    protected internal DirectiveTypeDescriptor(
        IDescriptorContext context,
        DirectiveTypeDefinition definition)
        : base(context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    protected internal override DirectiveTypeDefinition Definition { get; protected set; } = new();

    protected ICollection<DirectiveArgumentDescriptor> Arguments { get; } =
        new List<DirectiveArgumentDescriptor>();

    protected override void OnCreateDefinition(
        DirectiveTypeDefinition definition)
    {
        Context.Descriptors.Push(this);

        if (!Definition.AttributesAreApplied && Definition.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(
                Context,
                this,
                Definition.RuntimeType);
            Definition.AttributesAreApplied = true;
        }

        var arguments = new Dictionary<string, DirectiveArgumentDefinition>(StringComparer.Ordinal);
        var handledMembers = new HashSet<PropertyInfo>();

        FieldDescriptorUtilities.AddExplicitFields(
            Arguments.Select(t => t.CreateDefinition()),
            f => f.Property,
            arguments,
            handledMembers);

        OnCompleteArguments(arguments, handledMembers);

        definition.Arguments.AddRange(arguments.Values);

        base.OnCreateDefinition(definition);

        Context.Descriptors.Pop();
    }

    protected virtual void OnCompleteArguments(
        IDictionary<string, DirectiveArgumentDefinition> arguments,
        ISet<PropertyInfo> handledProperties)
    {
    }

    public IDirectiveTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public IDirectiveTypeDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    public IDirectiveArgumentDescriptor Argument(string name)
    {
        var descriptor = Arguments.FirstOrDefault(t => t.Definition.Name.EqualsOrdinal(name));

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
        Definition.Locations |= value;
        return this;
    }

    public IDirectiveTypeDescriptor Use(DirectiveMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        Definition.MiddlewareComponents.Add(middleware);
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
        Definition.IsRepeatable = true;
        return this;
    }

    public IDirectiveTypeDescriptor Public()
    {
        Definition.IsPublic = true;
        return this;
    }

    public IDirectiveTypeDescriptor Internal()
    {
        Definition.IsPublic = false;
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
        descriptor.Definition.RuntimeType = typeof(object);
        return descriptor;
    }

    public static DirectiveTypeDescriptor From(
        IDescriptorContext context,
        DirectiveTypeDefinition definition)
        => new(context, definition);

    public static DirectiveTypeDescriptor From<T>(
        IDescriptorContext context,
        DirectiveTypeDefinition definition)
        => new DirectiveTypeDescriptor<T>(context, definition);
}
