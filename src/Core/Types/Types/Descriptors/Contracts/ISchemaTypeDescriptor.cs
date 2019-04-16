using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface ISchemaTypeDescriptor
        : IDescriptor<SchemaTypeDefinition>
        , IFluent
    {
        ISchemaTypeDescriptor Name(NameString value);

        ISchemaTypeDescriptor Description(string value);

        ISchemaTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        ISchemaTypeDescriptor Directive<T>()
            where T : class, new();

        ISchemaTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
