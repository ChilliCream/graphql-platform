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
        => Setup.CreateContext(this, new());

    IDescriptorContext ISchemaBuilder.CreateContext()
        => CreateContext();

    ISchema ISchemaBuilder.Create()
        => Create();

    ISchema ISchemaBuilder.Create(IDescriptorContext context)
        => Create(context);
}
