using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Helpers;

internal static class FieldMergeHelper
{
    public static TDefinition MergeFields<TOperation, TDefinition>(
        this TOperation _,
        TDefinition source,
        TDefinition target,
        OperationContext context,
        Func<IReadOnlyList<FieldDefinitionNode>, TDefinition> action)
        where TOperation : ISchemaNodeOperation<TDefinition>
        where TDefinition : ComplexTypeDefinitionNodeBase
    {
        var fields = target.Fields
            .Concat(source.Fields)
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key,
                x => x.ToList());

        return MergeFields(context, action, fields);
    }

    public static TTargetDefinition MergeFields<TOperation, TSourceDefinition, TTargetDefinition>(
        this TOperation _,
        TSourceDefinition source,
        TTargetDefinition target,
        OperationContext context,
        Func<IReadOnlyList<FieldDefinitionNode>, TTargetDefinition> action)
        where TOperation : ISchemaNodeOperation<TSourceDefinition, TTargetDefinition>
        where TSourceDefinition : ComplexTypeDefinitionNodeBase
        where TTargetDefinition : ComplexTypeDefinitionNodeBase
    {
        var fields = target.Fields
            .Concat(source.Fields)
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key,
                x => x.ToList());

        return MergeFields(context, action, fields);
    }

    private static TDefinition MergeFields<TDefinition>(
        OperationContext context,
        Func<IReadOnlyList<FieldDefinitionNode>, TDefinition> action,
        Dictionary<NameNode, List<FieldDefinitionNode>> fields)
    {
        var updatedFields = new List<FieldDefinitionNode>();
        foreach (List<FieldDefinitionNode> group in fields.Values)
        {
            FieldDefinitionNode targetField = group.First();
            IEnumerable<FieldDefinitionNode> remaining = group.Skip(1);
            foreach (FieldDefinitionNode field in remaining)
            {
                ICollection<ISchemaNodeOperation> operations = context.OperationProvider.GetOperations(field);
                targetField = operations.Apply(field, targetField, context);
            }

            updatedFields.Add(targetField);
        }

        return action.Invoke(updatedFields);
    }
}
