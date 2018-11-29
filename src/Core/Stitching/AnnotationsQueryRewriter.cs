
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

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

    public class BrokeredQueryRewriter
        : QuerySyntaxRewriter<string>
    {


        protected override FieldNode VisitField(
            FieldNode node,
            string schemaName)
        {
            FieldNode current = node;

            current = current.WithDirectives(
                RemoveStitchingDirectives(current.Directives));

            return base.VisitField(current, schemaName);
        }

        protected override SelectionSetNode VisitSelectionSet(
            SelectionSetNode node,
            string schemaName)
        {
            var selections = new List<ISelectionNode>();

            foreach (ISelectionNode selection in node.Selections)
            {
                if ((IsRelevant(selection, schemaName)))
                {
                    selections.Add(selection);
                }
            }

            return base.VisitSelectionSet(
                node.WithSelections(selections),
                schemaName);
        }

        private bool IsRelevant(ISelectionNode selection, string schemaName)
        {
            return selection.Directives
                .Any(t => t.IsSchemaDirective(schemaName));
        }


        private IReadOnlyCollection<DirectiveNode> RemoveStitchingDirectives(
            IEnumerable<DirectiveNode> directives)
        {
            return directives.Where(t => !t.IsStitchingDirective()).ToList();
        }


    }

    internal static class AstStitchingExtensions
    {
        private static readonly HashSet<string> _stitchingDirectives =
            new HashSet<string>
            {
                    DirectiveNames.Schema,
                    DirectiveNames.Delegate
            };

        public static string GetSchemaName(this FieldNode field)
        {
            DirectiveNode directive = field.Directives
                .SingleOrDefault(t => IsSchemaDirective(t));

            if (directive == null)
            {
                throw new ArgumentException(
                    "The specified field is not annotated.");
            }

            ArgumentNode argument = directive.Arguments
                .SingleOrDefault(t => t.IsNameArgument());

            if (argument == null)
            {
                throw new ArgumentException(
                    "The schema directive has to have a name argument.");
            }

            if (argument.Value is StringValueNode value)
            {
                return value.Value;
            }

            throw new ArgumentException(
                "The schema directive name attribute " +
                "has to be a string value.");
        }

        public static bool IsSchemaDirective(
            this DirectiveNode directive)
        {
            return directive.Name.Value.EqualsOrdinal(DirectiveNames.Schema);
        }

        public static bool IsSchemaDirective(
            this DirectiveNode directive,
            string schemaName)
        {
            if (IsSchemaDirective(directive))
            {
                ArgumentNode argument = directive.Arguments
                    .SingleOrDefault(t => t.IsNameArgument());
                if (argument.Value is StringValueNode value
                    && value.Value.EqualsOrdinal(schemaName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsStitchingDirective(this DirectiveNode directive)
        {
            return _stitchingDirectives.Contains(directive.Name.Value);
        }

        private static bool IsNameArgument(this ArgumentNode argument)
        {
            return argument.Name.Value.EqualsOrdinal("name");
        }
    }


}
