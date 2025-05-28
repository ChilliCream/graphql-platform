using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

public interface ISchemaTypeDescriptor
    : IDescriptor<SchemaTypeConfiguration>
    , IFluent
{
    ISchemaTypeDescriptor Name(string value);

    ISchemaTypeDescriptor Description(string value);

    ISchemaTypeDescriptor Directive<T>(T directiveInstance) where T : class;

    ISchemaTypeDescriptor Directive<T>() where T : class, new();

    ISchemaTypeDescriptor Directive(string name, params ArgumentNode[] arguments);
}
