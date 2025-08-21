using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class RootSelectionSetPartitioner(FusionSchemaDefinition schema)
{
    public RootSelectionSetPartitionerResult Partition(RootSelectionSetPartitionerInput input)
    {
        var nodeFields = new List<FieldNode>();

        CollectNodeFields(
            schema,
            input.SelectionSet.Type,
            input.SelectionSet.Selections,
            nodeFields);

        return new RootSelectionSetPartitionerResult(null, nodeFields);

        static void CollectNodeFields(
            FusionSchemaDefinition compositeSchema,
            ITypeDefinition type,
            IReadOnlyList<ISelectionNode> selections,
            List<FieldNode> nodeFields)
        {
            foreach (var selection in selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (fieldNode.Name.Value == "node")
                        {
                            nodeFields.Add(fieldNode);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        var typeCondition = type;

                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeCondition = compositeSchema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                        }

                        CollectNodeFields(
                            compositeSchema,
                            typeCondition,
                            inlineFragmentNode.SelectionSet.Selections,
                            nodeFields);
                        break;
                }
            }
        }
    }
}
