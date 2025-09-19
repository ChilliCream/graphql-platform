using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types.Descriptors;

public class SchemaTypeDescriptor
    : DescriptorBase<SchemaTypeConfiguration>
    , ISchemaTypeDescriptor
{
    protected SchemaTypeDescriptor(IDescriptorContext context, Type type)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(type);
        Configuration.Name = context.Naming.GetTypeName(type);
    }

    protected SchemaTypeDescriptor(
        IDescriptorContext context,
        SchemaTypeConfiguration definition)
        : base(context)
    {
        Configuration = definition;
    }

    protected internal override SchemaTypeConfiguration Configuration { get; protected set; } = new();

    public ISchemaTypeDescriptor Name(string value)
    {
        Configuration.Name = value;
        return this;
    }

    public ISchemaTypeDescriptor Description(string value)
    {
        Configuration.Description = value;
        return this;
    }

    public ISchemaTypeDescriptor Directive<T>(T directiveInstance)
        where T : class
    {
        Configuration.GetLegacyConfiguration().AddDirective(directiveInstance, Context.TypeInspector);
        return this;
    }

    public ISchemaTypeDescriptor Directive<T>()
        where T : class, new()
    {
        Configuration.GetLegacyConfiguration().AddDirective(new T(), Context.TypeInspector);
        return this;
    }

    public ISchemaTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments)
    {
        Configuration.GetLegacyConfiguration().AddDirective(name, arguments);
        return this;
    }

    public static SchemaTypeDescriptor New(
        IDescriptorContext context,
        Type type) =>
        new SchemaTypeDescriptor(context, type);

    public static SchemaTypeDescriptor From(
        IDescriptorContext context,
        SchemaTypeConfiguration definition) =>
        new SchemaTypeDescriptor(context, definition);
}
