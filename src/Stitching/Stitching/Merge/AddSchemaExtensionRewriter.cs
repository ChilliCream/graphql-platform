using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    internal class AddSchemaExtensionRewriter
        : SchemaSyntaxRewriter<AddSchemaExtensionRewriter.MergeContext>
    {
        public DocumentNode AddExtensions(
            DocumentNode schema,
            DocumentNode extensions)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            var newTypes = extensions.Definitions
                .OfType<ITypeDefinitionNode>().ToList();

            DocumentNode current = schema;

            if (newTypes.Count > 0)
            {
                var definitions = schema.Definitions.ToList();
                definitions.AddRange(newTypes);
                current = current.WithDefinitions(definitions);
            }

            var context = new MergeContext(schema, extensions);
            current = RewriteDocument(current, context);
            return current;
        }

        protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            MergeContext context)
        {
            UnionTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out INamedTypeExtensionNode extension))
            {
                if (extension is UnionTypeExtensionNode unionTypeExtension)
                {
                    current = AddTypes(current, unionTypeExtension);
                    current = AddDirectives(current, unionTypeExtension,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
                }
            }

            return base.RewriteUnionTypeDefinition(current, context);
        }

        private static UnionTypeDefinitionNode AddTypes(
            UnionTypeDefinitionNode typeDefinition,
            UnionTypeExtensionNode typeExtension)
        {
            if (typeExtension.Types.Count == 0)
            {
                return typeDefinition;
            }

            var types =
                new OrderedDictionary<string, NamedTypeNode>();

            foreach (NamedTypeNode type in typeDefinition.Types)
            {
                types[type.Name.Value] = type;
            }

            foreach (NamedTypeNode type in typeExtension.Types)
            {
                types[type.Name.Value] = type;
            }

            return typeDefinition.WithTypes(types.Values.ToList());
        }

        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            MergeContext context)
        {
            ObjectTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out INamedTypeExtensionNode extension))
            {
                if (extension is ObjectTypeExtensionNode objectTypeExtension)
                {
                    current = AddFields(current, objectTypeExtension);
                    current = AddDirectives(current, objectTypeExtension,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
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
                out INamedTypeExtensionNode extension))
            {
                if (extension is InterfaceTypeExtensionNode ite)
                {
                    current = AddFields(current, ite);
                    current = AddDirectives(current, ite,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
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

        protected override InputObjectTypeDefinitionNode
            RewriteInputObjectTypeDefinition(
                InputObjectTypeDefinitionNode node,
                MergeContext context)
        {
            InputObjectTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out INamedTypeExtensionNode extension))
            {
                if (extension is InputObjectTypeExtensionNode iote)
                {
                    current = AddInputFields(current, iote);
                    current = AddDirectives(current, iote,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
                }
            }

            return base.RewriteInputObjectTypeDefinition(current, context);
        }

        private static InputObjectTypeDefinitionNode AddInputFields(
            InputObjectTypeDefinitionNode typeDefinition,
            InputObjectTypeExtensionNode typeExtension)
        {
            if (typeExtension.Fields.Count == 0)
            {
                return typeDefinition;
            }

            var fields =
                new OrderedDictionary<string, InputValueDefinitionNode>();

            foreach (InputValueDefinitionNode field in typeDefinition.Fields)
            {
                fields[field.Name.Value] = field;
            }

            foreach (InputValueDefinitionNode field in typeExtension.Fields)
            {
                // we allow an extension to override fields.
                fields[field.Name.Value] = field;
            }

            return typeDefinition.WithFields(fields.Values.ToList());
        }

        protected override EnumTypeDefinitionNode RewriteEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            MergeContext context)
        {
            EnumTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out INamedTypeExtensionNode extension))
            {
                if (extension is EnumTypeExtensionNode ete)
                {
                    current = AddEnumValues(current, ete);
                    current = AddDirectives(current, ete,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
                }
            }

            return base.RewriteEnumTypeDefinition(current, context);
        }

        private static EnumTypeDefinitionNode AddEnumValues(
            EnumTypeDefinitionNode typeDefinition,
            EnumTypeExtensionNode typeExtension)
        {
            if (typeExtension.Values.Count == 0)
            {
                return typeDefinition;
            }

            var values =
                new OrderedDictionary<string, EnumValueDefinitionNode>();

            foreach (EnumValueDefinitionNode value in typeDefinition.Values)
            {
                values[value.Name.Value] = value;
            }

            foreach (EnumValueDefinitionNode value in typeExtension.Values)
            {
                // we allow an extension to override values.
                values[value.Name.Value] = value;
            }

            return typeDefinition.WithValues(values.Values.ToList());
        }

        protected override ScalarTypeDefinitionNode RewriteScalarTypeDefinition(
            ScalarTypeDefinitionNode node,
            MergeContext context)
        {
            ScalarTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out INamedTypeExtensionNode extension))
            {
                if (extension is ScalarTypeExtensionNode ste)
                {
                    current = AddDirectives(current, ste,
                        d => current.WithDirectives(d), context);
                }
                else
                {
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
                }
            }

            return base.RewriteScalarTypeDefinition(current, context);
        }

        private static TDefinition AddDirectives<TDefinition, TExtension>(
            TDefinition typeDefinition,
            TExtension typeExtension,
            Func<IReadOnlyList<DirectiveNode>, TDefinition> withDirectives,
            MergeContext context)
            where TDefinition : NamedSyntaxNode, ITypeDefinitionNode
            where TExtension : NamedSyntaxNode, INamedTypeExtensionNode
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
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension,
                        string.Format(
                            CultureInfo.InvariantCulture, Resources
                            .AddSchemaExtensionRewriter_DirectiveDoesNotExist,
                            directive.Name.Value));
                }

                if (!alreadyDeclared.Add(directive.Name.Value)
                    && directiveDefinition.IsUnique)
                {
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension,
                        string.Format(
                            CultureInfo.InvariantCulture, Resources
                            .AddSchemaExtensionRewriter_DirectiveIsUnique,
                            directive.Name.Value));
                }

                directives.Add(directive);
            }

            return withDirectives.Invoke(directives);
        }

        public class MergeContext
        {
            public MergeContext(DocumentNode schema, DocumentNode extensions)
            {
                Extensions = extensions.Definitions
                    .OfType<INamedTypeExtensionNode>()
                    .ToDictionary(t => t.Name.Value);

                Directives = schema.Definitions
                    .OfType<DirectiveDefinitionNode>()
                    .ToDictionary(t => t.Name.Value);
            }

            public IDictionary<string, INamedTypeExtensionNode> Extensions
            { get; }

            public IDictionary<string, DirectiveDefinitionNode> Directives
            { get; }
        }
    }
}
