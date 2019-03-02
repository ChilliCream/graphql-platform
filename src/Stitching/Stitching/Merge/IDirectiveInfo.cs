using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface IDirectiveInfo
    {
        DirectiveDefinitionNode Definition { get; }

        ISchemaInfo Schema { get; }
    }
}
