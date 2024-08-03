using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

public class CompositeSchemaBuilder
{
    private Dictionary<string, ITypeDefinitionNode> _typeDefinitionNodes = new();
    private List<ICompositeType> _types = new();

    private void Complete(CompositeObjectType type)
    {
        //type.Complete();


    }
}
