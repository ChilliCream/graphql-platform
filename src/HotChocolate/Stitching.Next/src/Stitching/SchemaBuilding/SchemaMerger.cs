using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

internal class SchemaMerger
{
    private readonly CleanQueryTypeRewriter _cleanQueryTypeRewriter = new();
    private CleanQueryTypeContext? _cleanQueryTypeContext;

    public SchemaInfo Merge(IReadOnlyList<SchemaInfo> schemaInfos)
    {
        if (schemaInfos is null)
        {
            throw new ArgumentNullException(nameof(schemaInfos));
        }

        if (schemaInfos.Count == 0)
        {
            throw new ArgumentException(
                "Only schema documents are allowed.",
                nameof(schemaInfos));
        }

        SchemaInfo root = schemaInfos[0];
        CleanSchema(root);

        for (int i = 1; i < schemaInfos.Count; i++)
        {
            SchemaInfo next = schemaInfos[i];
            CleanSchema(next);
            Merge(next, root);
        }

        root.Name = "Merged";
        return root;
    }

    public void Merge(SchemaInfo source, SchemaInfo target)
    {
        if (source.Query is not null && target.Query is not null)
        {
            MergeType(source.Query, target.Query);
        }

        if (source.Mutation is not null && target.Mutation is not null)
        {
            MergeType(source.Mutation, target.Mutation);
        }

        if (source.Subscription is not null && target.Subscription is not null)
        {
            MergeType(source.Subscription, target.Subscription);
        }

        foreach (ObjectTypeInfo sourceType in source.Types.Values.OfType<ObjectTypeInfo>())
        {
            if (target.Types.TryGetValue(sourceType.Name, out var value))
            {
                if (value is ObjectTypeInfo targetType)
                {
                    MergeType(sourceType, targetType);
                }
            }
            else
            {
                target.Types.Add(sourceType.Name, sourceType);
            }
        }
    }

    private void MergeType(ObjectTypeInfo source, ObjectTypeInfo target)
    {
        var processed = new HashSet<string>();
        var temp = new List<FieldDefinitionNode>();

        foreach (ObjectFetcherInfo fetcher in source.Fetchers)
        {
            target.Fetchers.Add(fetcher);
        }

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
    }

    private void CleanSchema(SchemaInfo schemaInfo)
    {
        if (schemaInfo.Query is not null)
        {
            CleanRootType(schemaInfo.Query);
        }
    }

    private void CleanRootType(ObjectTypeInfo rootType)
    {
        CleanQueryTypeContext context =
            Interlocked.Exchange(ref _cleanQueryTypeContext, null) ??
                new();

        var rewritten =
            (ObjectTypeDefinitionNode)_cleanQueryTypeRewriter.Rewrite(
                rootType.Definition,
                context);
        rootType.Definition = rewritten;

        context.Clear();
        Interlocked.Exchange(ref _cleanQueryTypeContext, context);
    }

    private class CleanQueryTypeRewriter : SchemaSyntaxRewriter<CleanQueryTypeContext>
    {
        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            CleanQueryTypeContext context)
        {
            if (node.Fields.Count > 0)
            {
                List<FieldDefinitionNode> temp = context.Fields;
                temp.AddRange(node.Fields);

                int i = 0;
                while (i < temp.Count)
                {
                    FieldDefinitionNode field = temp[i];

                    if (!InternalDirective.HasOne(field))
                    {
                        i++;
                        continue;
                    }

                    temp.RemoveAt(i);
                }

                IReadOnlyList<FieldDefinitionNode> fields = temp switch
                {
                    { Count: 0 } => Array.Empty<FieldDefinitionNode>(),
                    { Count: 1 } => new FieldDefinitionNode[] { temp[0] },
                    { Count: 2 } => new FieldDefinitionNode[] { temp[0], temp[1] },
                    _ => temp.ToArray()
                };

                node = node.WithFields(fields);
                temp.Clear();
            }


            return base.RewriteObjectTypeDefinition(node, context);
        }

        protected override InputValueDefinitionNode RewriteInputValueDefinition(
            InputValueDefinitionNode node,
            CleanQueryTypeContext context)
        {
            if (node.Directives.Count > 0)
            {
                List<DirectiveNode> temp = context.Directives;
                temp.AddRange(node.Directives);

                int i = 0;
                while (i < temp.Count)
                {
                    DirectiveNode directive = temp[i];

                    if (!IsDirective.IsOf(directive))
                    {
                        i++;
                        continue;
                    }

                    temp.RemoveAt(i);
                }

                IReadOnlyList<DirectiveNode> directives = temp switch
                {
                    { Count: 0 } => Array.Empty<DirectiveNode>(),
                    { Count: 1 } => new DirectiveNode[] { temp[0] },
                    { Count: 2 } => new DirectiveNode[] { temp[0], temp[1] },
                    _ => temp.ToArray()
                };

                node = node.WithDirectives(directives);
                temp.Clear();
            }

            return base.RewriteInputValueDefinition(node, context);
        }
    }

    private class CleanQueryTypeContext
    {
        public List<FieldDefinitionNode> Fields { get; } = new();

        public List<DirectiveNode> Directives { get; } = new();

        public void Clear()
        {
            Fields.Clear();
            Directives.Clear();
        }
    }
}
