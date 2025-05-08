#nullable enable

using HotChocolate.Types.Descriptors;

namespace HotChocolate;

public partial class SchemaBuilder
{
    public Schema Create() => Setup.Create(this);

    public Schema Create(IDescriptorContext context)
    {
        if (context is DescriptorContext casted)
        {
            return Setup.Create(this, casted.Schema, casted);
        }

        throw new NotSupportedException("Context not supported.");
    }

    public DescriptorContext CreateContext()
        => Setup.CreateContext(this, new LazySchema());

    IDescriptorContext ISchemaBuilder.CreateContext()
        => CreateContext();

    Schema ISchemaBuilder.Create()
        => Create();

    Schema ISchemaBuilder.Create(IDescriptorContext context)
        => Create(context);
}
