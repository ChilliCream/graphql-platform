using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Delegation.ScopedVariables;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class RootTypeMergeHandler
         : ITypeMergeHandler
    {
        private readonly MergeTypeRuleDelegate _next;

        public RootTypeMergeHandler(MergeTypeRuleDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.Count > 0)
            {
                if (types[0].IsRootType)
                {
                    var names = new HashSet<string>();
                    var fields = new List<FieldDefinitionNode>();

                    foreach (ObjectTypeInfo type in
                        types.OfType<ObjectTypeInfo>())
                    {
                        IntegrateFields(type.Definition, type, names, fields);
                    }

                    if (types[0].Schema.TryGetOperationType(
                        (ObjectTypeDefinitionNode)types[0].Definition,
                        out OperationType operationType))
                    {
                        var mergedRootType = new ObjectTypeDefinitionNode
                        (
                            null,
                            new NameNode(operationType.ToString()),
                            null,
                            Array.Empty<DirectiveNode>(),
                            Array.Empty<NamedTypeNode>(),
                            fields
                        );
                        context.AddType(mergedRootType);
                    }
                }
                else
                {
                    _next.Invoke(context, types);
                }
            }
        }

        private static void IntegrateFields(
            ObjectTypeDefinitionNode rootType,
            ITypeInfo typeInfo,
            ISet<string> names,
            ICollection<FieldDefinitionNode> fields)
        {
            string schemaName = typeInfo.Schema.Name;

            foreach (FieldDefinitionNode field in rootType.Fields)
            {
                FieldDefinitionNode current = field;

                if (names.Add(current.Name.Value))
                {
                    current = current.AddDelegationPath(schemaName);
                }
                else
                {
                    var path = new SelectionPathComponent(
                        field.Name,
                        field.Arguments.Select(t => new ArgumentNode(
                            t.Name,
                            new ScopedVariableNode(
                                null,
                                new NameNode(ScopeNames.Arguments),
                                t.Name))).ToList());

                    var newName = new NameNode(
                        typeInfo.CreateUniqueName(current));

                    current = current.WithName(newName)
                        .AddDelegationPath(schemaName, path);
                }

                fields.Add(current);
            }
        }
    }
}
