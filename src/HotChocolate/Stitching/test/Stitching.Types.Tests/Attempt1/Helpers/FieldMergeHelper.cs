using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Operations;

namespace HotChocolate.Stitching.Types.Attempt1.Helpers;

internal static class FieldMergeHelper
{
    public static TDefinition MergeFields<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target,
        MergeOperationContext context)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : ComplexTypeDefinitionNodeBase, IHasWithFields<TDefinition>
    {
        var fields = target.Fields
            .Concat(source.Fields)
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key,
                x => x.ToList());

        return MergeFields(target, context, fields);
    }

    public static TTargetDefinition MergeFields<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target,
        MergeOperationContext context)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : ComplexTypeDefinitionNodeBase
        where TTargetDefinition : ComplexTypeDefinitionNodeBase, IHasWithFields<TTargetDefinition>
    {
        var fields = target.Fields
            .Concat(source.Fields)
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key,
                x => x.ToList());

        return MergeFields(target, context, fields);
    }

    private static TDefinition MergeFields<TDefinition>(
        TDefinition target,
        MergeOperationContext context,
        Dictionary<NameNode, List<FieldDefinitionNode>> fields)
        where TDefinition : IHasWithFields<TDefinition>
    {
        var updatedFields = new List<FieldDefinitionNode>();
        foreach (List<FieldDefinitionNode> group in fields.Values)
        {
            FieldDefinitionNode targetField = group.First();
            IEnumerable<FieldDefinitionNode> remaining = group.Skip(1);
            foreach (FieldDefinitionNode field in remaining)
            {
                ICollection<IMergeSchemaNodeOperation> operations = context.OperationProvider
                    .GetOperations(field);

                targetField = operations.Apply(field, targetField, context);
            }

            updatedFields.Add(targetField);
        }

        return target.WithFields(updatedFields);
    }
}
