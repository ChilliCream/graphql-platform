using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class SchemaTypeDescriptor
    : DescriptorBase<SchemaTypeDefinition>
    , ISchemaTypeDescriptor
{
    protected SchemaTypeDescriptor(IDescriptorContext context, Type type)
        : base(context)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        Definition.Name = context.Naming.GetTypeName(type);
    }

    protected SchemaTypeDescriptor(
        IDescriptorContext context,
        SchemaTypeDefinition definition)
        : base(context)
    {
        Definition = definition;
    }

    protected internal override SchemaTypeDefinition Definition { get; protected set; } = new();

    public ISchemaTypeDescriptor Name(string value)
    {
        Definition.Name = value;
        return this;
    }

    public ISchemaTypeDescriptor Description(string value)
    {
        Definition.Description = value;
        return this;
    }

    public ISchemaTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Definition.GetLegacyDefinition().AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public ISchemaTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Definition.GetLegacyDefinition().AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public ISchemaTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Definition.GetLegacyDefinition().AddDirective(name, arguments);
        return this;
    }

    public static SchemaTypeDescriptor New(
        IDescriptorContext context,
        Type type) =>
        new SchemaTypeDescriptor(context, type);

    public static SchemaTypeDescriptor From(
        IDescriptorContext context,
        SchemaTypeDefinition definition) =>
        new SchemaTypeDescriptor(context, definition);
}
