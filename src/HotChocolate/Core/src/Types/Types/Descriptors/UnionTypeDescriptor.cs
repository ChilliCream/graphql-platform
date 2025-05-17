using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class UnionTypeDescriptor
    : DescriptorBase<UnionTypeConfiguration>
    , IUnionTypeDescriptor
{
    protected UnionTypeDescriptor(IDescriptorContext context, Type clrType)
        : base(context)
    {
        if (clrType is null)
        {
            throw new ArgumentNullException(nameof(clrType));
        }

        Configuration.RuntimeType = clrType;
        Configuration.Name = context.Naming.GetTypeName(clrType, TypeKind.Union);
        Configuration.Description = context.Naming.GetTypeDescription(clrType, TypeKind.Union);
    }

    protected UnionTypeDescriptor(
        IDescriptorContext context,
        UnionTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition;
    }

    protected UnionTypeDescriptor(IDescriptorContext context)
        : base(context)
    {
        Configuration.RuntimeType = typeof(object);
    }

    protected internal override UnionTypeConfiguration Configuration { get; protected set; } = new();

    protected override void OnCreateConfiguration(UnionTypeConfiguration definition)
    {
        Context.Descriptors.Push(this);

        if (!Configuration.AttributesAreApplied && Configuration.RuntimeType != typeof(object))
        {
            Context.TypeInspector.ApplyAttributes(Context, this, Configuration.RuntimeType);
            Configuration.AttributesAreApplied = true;
        }

        base.OnCreateConfiguration(definition);

        Context.Descriptors.Pop();
    }

    public IUnionTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public IUnionTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public IUnionTypeDescriptor Type<TObjectType>()
        where TObjectType : ObjectType
    {
        Configuration.Types.Add(
            Context.TypeInspector.GetTypeRef(typeof(TObjectType), TypeContext.Output));
        return this;
    }

    public IUnionTypeDescriptor Type<TObjectType>(TObjectType objectType)
        where TObjectType : ObjectType
    {
        if (objectType is null)
        {
            throw new ArgumentNullException(nameof(objectType));
        }
        Configuration.Types.Add(TypeReference.Create(objectType));
        return this;
    }

    public IUnionTypeDescriptor Type(NamedTypeNode objectType)
    {
        if (objectType is null)
        {
            throw new ArgumentNullException(nameof(objectType));
        }
        Configuration.Types.Add(TypeReference.Create(objectType, TypeContext.Output));
        return this;
    }

    public IUnionTypeDescriptor ResolveAbstractType(
        ResolveAbstractType resolveAbstractType)
    {
        Configuration.ResolveAbstractType = resolveAbstractType
           ?? throw new ArgumentNullException(nameof(resolveAbstractType));
        return this;
    }

    public IUnionTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public IUnionTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public IUnionTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Configuration.AddDirective(name, arguments);
        return this;
    }

    public static UnionTypeDescriptor New(
        IDescriptorContext context,
        Type clrType) =>
        new(context, clrType);

    public static UnionTypeDescriptor New(
        IDescriptorContext context) =>
        new(context);

    public static UnionTypeDescriptor FromSchemaType(
        IDescriptorContext context,
        Type schemaType)
    {
        var descriptor = New(context, schemaType);
        descriptor.Configuration.RuntimeType = typeof(object);
        return descriptor;
    }

    public static UnionTypeDescriptor From(
        IDescriptorContext context,
        UnionTypeConfiguration definition) =>
        new(context, definition);
}
