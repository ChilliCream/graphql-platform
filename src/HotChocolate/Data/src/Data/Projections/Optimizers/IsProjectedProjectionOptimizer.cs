using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Projections.ProjectionConvention;

namespace HotChocolate.Data.Projections.Handlers
{
    public class IsProjectedProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.DeclaringType is ObjectType objectType &&
            objectType.ContextData.ContainsKey(AlwaysProjectedFieldsKey);

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            if (context.Type is ObjectType type &&
                type.ContextData.TryGetValue(AlwaysProjectedFieldsKey, out var fieldsObj) &&
                fieldsObj is string[] fields)
            {
                return selection;
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var alias = "__projection_alias_" + i;

                // if the field is already in the selection set we do not need to project it
                if (context.Fields.TryGetValue(fields[i], out var field) &&
                    field.Field.Name == fields[i])
                {
                    if (!context.Fields.TryGetValue(fields[i], out var field) ||
                        field.Field.Name != fields[i])
                    {
                        IObjectField nodesField = type.Fields[fields[i]];
                        var alias = "__projection_alias_" + aliasCount++;
                        var nodesFieldNode = new FieldNode(
                            null,
                            new NameNode(fields[i]),
                            new NameNode(alias),
                            Array.Empty<DirectiveNode>(),
                            Array.Empty<ArgumentNode>(),
                            null);

                        var compiledSelection = new Selection(
                            context.GetNextId(),
                            context.Type,
                            nodesField,
                            nodesFieldNode,
                            context.CompileResolverPipeline(nodesField, nodesFieldNode),
                            arguments: selection.Arguments,
                            internalSelection: true);

                        context.Fields[alias] = compiledSelection;
                    }
                }

                // if the field is already added as an alias we do not need to add it
                if (context.Fields.TryGetValue(alias, out field) &&
                    field.Field.Name == fields[i])
                {
                    continue;
                }

                IObjectField nodesField = type.Fields[fields[i]];
                var nodesFieldNode = new FieldNode(
                    null,
                    new NameNode(fields[i]),
                    new NameNode(alias),
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null);

                FieldDelegate nodesPipeline =
                    context.CompileResolverPipeline(nodesField, nodesFieldNode);

                var compiledSelection = new Selection(
                    context.Type,
                    nodesField,
                    nodesFieldNode,
                    nodesPipeline,
                    arguments: selection.Arguments,
                    internalSelection: true);

                context.Fields[alias] = compiledSelection;
            }

            return selection;
        }
    }
}
