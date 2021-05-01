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
                for (var i = 0; i < fields.Length; i++)
                {
                    if (!context.Fields.ContainsKey(fields[i]))
                    {
                        IObjectField nodesField = type.Fields[fields[i]];
                        var nodesFieldNode = new FieldNode(
                            null,
                            new NameNode(fields[i]),
                            null,
                            Array.Empty<DirectiveNode>(),
                            Array.Empty<ArgumentNode>(),
                            null);

                        var compiledSelection = new Selection(
                            context.Type,
                            nodesField,
                            nodesFieldNode,
                            context.CompileResolverPipeline(nodesField, nodesFieldNode),
                            arguments: selection.Arguments,
                            internalSelection: true);

                        context.Fields[fields[i]] = compiledSelection;
                    }
                }
            }

            return selection;
        }
    }
}
