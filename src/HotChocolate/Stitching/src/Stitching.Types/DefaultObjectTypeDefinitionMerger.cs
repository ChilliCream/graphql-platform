using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal sealed class DefaultObjectTypeDefinitionMerger : TypeDefinitionMerger<ObjectTypeDefinition>
{
    protected override void MergeInto(ObjectTypeDefinition source, ObjectTypeDefinition target)
    {
        var processed = new HashSet<string>();
        var temp = new List<FieldDefinitionNode>();

        foreach (FieldDefinitionNode field in target.Definition.Fields)
        {
            temp.Add(field);
            processed.Add(field.Name.Value);
        }

        foreach (FieldDefinitionNode targetField in source.Definition.Fields)
        {
            if (processed.Add(targetField.Name.Value))
            {
                temp.Add(targetField);
            }
        }

        target.Definition = target.Definition.WithFields(temp);
        source.Bindings.CopyTo(target.Bindings);
    }
}
