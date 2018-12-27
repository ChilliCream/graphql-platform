using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal sealed class AnnotateQueryRewriter
        : QuerySyntaxRewriter<AnnotationContext>
    {
        private static readonly HashSet<NameString> _stitchingDirectives =
            new HashSet<NameString>
            {
                DirectiveNames.Schema,
                DirectiveNames.Delegate
            };

        protected override OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node,
            AnnotationContext context)
        {
            ObjectType rootType = context.Schema
                .GetOperationType(node.Operation);

            return base.RewriteOperationDefinition(
                node, context.WithType(rootType));
        }


        protected override FieldNode RewriteField(
            FieldNode node,
            AnnotationContext context)
        {
            string fieldName = node.Name.Value;

            if (context.SelectedType is IComplexOutputType type
                && type.Fields.TryGetField(fieldName, out IOutputField field))
            {

                ILookup<NameString, IDirective> directiveLookup =
                    field.Directives.ToLookup(t => t.Name);

                var directives = new List<DirectiveNode>(node.Directives);

                foreach (var group in directiveLookup)
                {
                    if (_stitchingDirectives.Contains(group.Key))
                    {
                        directives.AddRange(group.Select(t => t.ToNode()));
                    }
                }

                return base.RewriteField(
                    node.WithDirectives(directives),
                    context.WithType(field.Type.NamedType()));
            }

            return node;
        }
    }
}
