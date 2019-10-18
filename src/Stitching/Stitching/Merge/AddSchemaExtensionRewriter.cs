using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    public class AddSchemaExtensionRewriter
        : SchemaSyntaxRewriter<AddSchemaExtensionRewriter.MergeContext>
    {
        private readonly Dictionary<string, DirectiveDefinitionNode> _gloabalDirectives;

        public AddSchemaExtensionRewriter()
        {
            _gloabalDirectives = new Dictionary<string, DirectiveDefinitionNode>();
        }

        public AddSchemaExtensionRewriter(IEnumerable<DirectiveDefinitionNode> gloabalDirectives)
        {
            if (gloabalDirectives is null)
            {
                throw new ArgumentNullException(nameof(gloabalDirectives));
            }

            _gloabalDirectives = gloabalDirectives.ToDictionary(t => t.Name.Value);
        }

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
            var newDirectives = extensions.Definitions
                .OfType<DirectiveDefinitionNode>().ToList();

            DocumentNode current = schema;

            if (newTypes.Count > 0 || newDirectives.Count > 0)
            {
                current = RemoveDirectives(current,
                    newDirectives.Select(t => t.Name.Value));
                current = RemoveTypes(current,
                    newTypes.Select(t => t.Name.Value));

                var definitions = schema.Definitions.ToList();
                definitions.AddRange(newTypes);
                definitions.AddRange(newDirectives);
                current = current.WithDefinitions(definitions);
            }

            var context = new MergeContext(current, extensions);
            current = RewriteDocument(current, context);
            return current;
        }

        private static DocumentNode RemoveDirectives(
            DocumentNode document,
            IEnumerable<string> directiveNames)
        {
            return RemoveDefinitions(
                document,
                d => d.Definitions.OfType<DirectiveDefinitionNode>()
                    .ToDictionary(t => t.Name.Value, t => (IDefinitionNode)t),
                directiveNames);
        }

        private static DocumentNode RemoveTypes(
            DocumentNode document,
            IEnumerable<string> directiveNames)
        {
            return RemoveDefinitions(
                document,
                d => d.Definitions.OfType<ITypeDefinitionNode>()
                    .ToDictionary(t => t.Name.Value, t => (IDefinitionNode)t),
                directiveNames);
        }

        private static DocumentNode RemoveDefinitions(
            DocumentNode document,
            Func<DocumentNode, Dictionary<string, IDefinitionNode>> toDict,
            IEnumerable<string> names)
        {
            List<IDefinitionNode> definitions = document.Definitions.ToList();
            Dictionary<string, IDefinitionNode> directives = toDict(document);

            foreach (string name in names)
            {
                if (directives.TryGetValue(name, out IDefinitionNode directive))
                {
                    definitions.Remove(directive);
                }
            }

            return document.WithDefinitions(definitions);
        }

        protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            MergeContext context)
        {
            UnionTypeDefinitionNode current = node;

            if (context.Extensions.TryGetValue(
                current.Name.Value,
                out ITypeExtensionNode extension))
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
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
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
                out ITypeExtensionNode extension))
            {
                if (extension is ObjectTypeExtensionNode objectTypeExtension)
                {

                    current = AddInterfaces(current, objectTypeExtension);
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
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
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

        private static ObjectTypeDefinitionNode AddInterfaces(
            ObjectTypeDefinitionNode typeDefinition,
            ObjectTypeExtensionNode typeExtension)
        {
            if (typeExtension.Interfaces.Count == 0)
            {
                return typeDefinition;
            }

            var interfaces = new HashSet<string>(
                typeDefinition.Interfaces.Select(t => t.Name.Value));

            foreach (string interfaceName in
                typeExtension.Interfaces.Select(t => t.Name.Value))
            {
                interfaces.Add(interfaceName);
            }

            if (interfaces.Count == typeDefinition.Interfaces.Count)
            {
                return typeDefinition;
            }

            return typeDefinition.WithInterfaces(
                interfaces.Select(n => new NamedTypeNode(new NameNode(n)))
                    .ToList());
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
                    throw new SchemaMergeException(
                        current,
                        extension,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
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
                out ITypeExtensionNode extension))
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
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
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
                out ITypeExtensionNode extension))
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
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
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
                out ITypeExtensionNode extension))
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
                            StitchingResources
                                .AddSchemaExtensionRewriter_TypeMismatch,
                            node.Name.Value,
                            node.Kind,
                            extension.Kind));
                }
            }

            return base.RewriteScalarTypeDefinition(current, context);
        }

        private TDefinition AddDirectives<TDefinition, TExtension>(
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
                if (!_gloabalDirectives.TryGetValue(directive.Name.Value,
                    out DirectiveDefinitionNode directiveDefinition)
                    && !context.Directives.TryGetValue(directive.Name.Value,
                        out directiveDefinition))
                {
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension,
                        string.Format(
                            CultureInfo.InvariantCulture, StitchingResources
                            .AddSchemaExtensionRewriter_DirectiveDoesNotExist,
                            directive.Name.Value));
                }

                if (!alreadyDeclared.Add(directive.Name.Value)
                    && directiveDefinition.IsUnique)
                {
                    throw new SchemaMergeException(
                        typeDefinition, typeExtension,
                        string.Format(
                            CultureInfo.InvariantCulture, StitchingResources
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
                    .OfType<ITypeExtensionNode>()
                    .ToDictionary(t => t.Name.Value);

                Directives = schema.Definitions
                    .OfType<DirectiveDefinitionNode>()
                    .ToDictionary(t => t.Name.Value);
            }

            public IDictionary<string, ITypeExtensionNode> Extensions { get; }

            public IDictionary<string, DirectiveDefinitionNode> Directives { get; }
        }
    }
}
