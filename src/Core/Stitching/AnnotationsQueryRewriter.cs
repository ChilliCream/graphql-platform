
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal sealed class AnnotationsQueryRewriter
        : QuerySyntaxRewriter<AnnotationContext>
    {
        private static readonly HashSet<string> _stitchingDirectives =
            new HashSet<string>
            {
                DirectiveNames.Schema,
                DirectiveNames.Delegate
            };

        protected override OperationDefinitionNode VisitOperationDefinition(
            OperationDefinitionNode node,
            AnnotationContext context)
        {
            ObjectType rootType = context.Schema
                .GetOperationType(node.Operation);

            return base.VisitOperationDefinition(
                node, context.WithType(rootType));
        }


        protected override FieldNode VisitField(
            FieldNode node,
            AnnotationContext context)
        {
            string fieldName = node.Name.Value;

            if (context.SelectedType is IComplexOutputType type
                && type.Fields.TryGetField(fieldName, out IOutputField field))
            {

                ILookup<string, IDirective> directiveLookup =
                    field.Directives.ToLookup(t => t.Name);

                var directives = new List<DirectiveNode>(node.Directives);

                foreach (var group in directiveLookup)
                {
                    if (_stitchingDirectives.Contains(group.Key))
                    {
                        directives.AddRange(group.Select(t => t.ToNode()));
                    }
                }

                return base.VisitField(
                    node.WithDirectives(directives),
                    context.WithType(field.Type.NamedType()));
            }

            return node;
        }
    }

    internal static class DirectiveNames
    {
        public const string Schema = "schema";
        public const string Delegate = "delegate";
    }

    public class SchemaDirective
    {
        public string Name { get; set; }
    }

    public class SchemaDirectiveType
        : DirectiveType<SchemaDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<SchemaDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Schema);
            descriptor.Location(Types.DirectiveLocation.FieldDefinition)
                .Location(Types.DirectiveLocation.Field);
        }
    }

    public class DelegateDirective
    {
        public string Operation { get; set; } = OperationType.Query.ToString();

        public string Path { get; set; }
    }

    public class DelegateDirectiveType
        : DirectiveType<DelegateDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DelegateDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Delegate);
            descriptor.Location(Types.DirectiveLocation.FieldDefinition)
                .Location(Types.DirectiveLocation.Field);
        }
    }
}
