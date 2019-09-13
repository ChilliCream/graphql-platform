using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface IDirectiveTypeInfo
    {
        DirectiveDefinitionNode Definition { get; }
        ISchemaInfo Schema { get; }
    }
}
