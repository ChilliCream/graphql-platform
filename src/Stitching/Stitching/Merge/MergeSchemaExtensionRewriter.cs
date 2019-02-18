using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Merge
{
    internal class MergeSchemaExtensionRewriter
        : SchemaSyntaxRewriter<MergeSchemaExtensionRewriter.MergeContext>
    {
        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            MergeContext context)
        {
            ObjectTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out ITypeExtensionNode extension))
            {
                if (extension is ObjectTypeExtensionNode objectTypeExtension)
                {
                    current = AddFields(current, objectTypeExtension);
                    current = AddDirectives(current, objectTypeExtension,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    // TODO : Resources
                    throw new SchemaMergeException(
                        current, extension, "EXTENSION TYPE DOES NOT MATCH");
                }
            }

            return base.RewriteObjectTypeDefinition(current, context);
        }

        private static ObjectTypeDefinitionNode AddFields(
            ObjectTypeDefinitionNode typeDefinition,
            ObjectTypeExtensionNode typeExtension)
        {
            IReadOnlyList<FieldDefinitionNode> fields =
                AddFields(typeDefinition.Fields, typeExtension.Fields);

            return fields == typeDefinition.Fields
                ? typeDefinition
                : typeDefinition.WithFields(fields);
        }

        protected override InterfaceTypeDefinitionNode
            RewriteInterfaceTypeDefinition(
                InterfaceTypeDefinitionNode node,
                MergeContext context)
        {
            InterfaceTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out ITypeExtensionNode extension))
            {
                if (extension is InterfaceTypeExtensionNode ite)
                {
                    current = AddFields(current, ite);
                    current = AddDirectives(current, ite,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    // TODO : Resources
                    throw new SchemaMergeException(
                        current, extension, "EXTENSION TYPE DOES NOT MATCH");
                }
            }

            return base.RewriteInterfaceTypeDefinition(current, context);
        }

        private static InterfaceTypeDefinitionNode AddFields(
            InterfaceTypeDefinitionNode typeDefinition,
            InterfaceTypeExtensionNode typeExtension)
        {
            IReadOnlyList<FieldDefinitionNode> fields =
                AddFields(typeDefinition.Fields, typeExtension.Fields);

            return fields == typeDefinition.Fields
                ? typeDefinition
                : typeDefinition.WithFields(fields);
        }

        private static IReadOnlyList<FieldDefinitionNode> AddFields(
            IReadOnlyList<FieldDefinitionNode> typeDefinitionFields,
            IReadOnlyList<FieldDefinitionNode> typeExtensionFields)
        {
            if (typeExtensionFields.Count == 0)
            {
                return typeDefinitionFields;
            }

            var fields = new OrderedDictionary<string, FieldDefinitionNode>();

            foreach (FieldDefinitionNode field in typeDefinitionFields)
            {
                fields[field.Name.Value] = field;
            }

            foreach (FieldDefinitionNode field in typeExtensionFields)
            {
                // we allow an extension to override fields.
                fields[field.Name.Value] = field;
            }

            return fields.Values.ToList();
        }

        private static TDefinition AddDirectives<TDefinition, TExtension>(
            TDefinition typeDefinition,
            TExtension typeExtension,
            Func<IReadOnlyList<DirectiveNode>, TDefinition> withDirectives,
            MergeContext context)
            where TDefinition : NamedSyntaxNode, ITypeDefinitionNode
            where TExtension : NamedSyntaxNode, ITypeExtensionNode
        {
            if (typeExtension.Directives.Count == 0)
            {
                return typeDefinition;
            }

            var alreadyDeclared = new HashSet<string>(
                typeDefinition.Directives.Select(t => t.Name.Value));
            var directives = new List<DirectiveNode>();

            foreach (DirectiveNode directive in typeExtension.Directives)
            {
                if (!context.Directives.TryGetValue(directive.Name.Value,
                    out DirectiveDefinitionNode directiveDefinition))
                {
                    // TODO : Resources
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension,
                        "DIRECTIVE DOES NOT EXIST");
                }

                if (alreadyDeclared.Add(directive.Name.Value)
                    && directiveDefinition.IsUnique)
                {
                    // TODO : Resources
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension, "DIRECTIVE IS UNIQUE");
                }

                directives.Add(directive);
            }

            return withDirectives.Invoke(directives);
        }

        public class MergeContext
        {
            public IDictionary<string, ITypeExtensionNode> Extensions { get; }
            public IDictionary<string, DirectiveDefinitionNode> Directives
            { get; }
        }
    }
}
