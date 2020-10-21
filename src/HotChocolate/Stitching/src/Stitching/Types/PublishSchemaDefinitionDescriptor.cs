using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Types
{
    public class PublishSchemaDefinitionDescriptor : IPublishSchemaDefinitionDescriptor
    {
        private readonly IRequestExecutorBuilder _builder;
        private readonly string _key = Guid.NewGuid().ToString();
        private readonly List<DirectiveNode> _schemaDirectives = new List<DirectiveNode>();
        private NameString _name;

        public PublishSchemaDefinitionDescriptor(IRequestExecutorBuilder builder)
        {
            _builder = builder;
        }

        public IPublishSchemaDefinitionDescriptor SetName(NameString name)
        {
            _name = name;
            return this;
        }

        public IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            _builder.ConfigureSchemaAsync(
                async (s, ct) =>
                {
#if NETSTANDARD2_0
                    byte[] content = await Task
                        .Run(() => File.ReadAllBytes(fileName), ct)
                        .ConfigureAwait(false);
#else
                    byte[] content = await File
                        .ReadAllBytesAsync(fileName, ct)
                        .ConfigureAwait(false);
#endif

                    s.AddTypeExtensions(Utf8GraphQLParser.Parse(content), _key);
                });

            return this;
        }

        public IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromResource(
            Assembly assembly,
            string key)
        {
            _builder.ConfigureSchemaAsync(
                async (s, ct) =>
                {
                    Stream? stream = assembly.GetManifestResourceStream(key);

                    if (stream is null)
                    {
                        // todo : throw helper
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage(
                                    "The resource `{0}` was not found!",
                                    key)
                                .Build());
                    }

                    using (stream)
                    {
                        var buffer = new byte[stream.Length];
                        await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                        s.AddTypeExtensions(Utf8GraphQLParser.Parse(buffer), _key);
                    }
                });

            return this;
        }

        public IPublishSchemaDefinitionDescriptor AddTypeExtensionsFromString(string schemaSdl)
        {
            _builder.ConfigureSchema(
                s => s.AddTypeExtensions(Utf8GraphQLParser.Parse(schemaSdl), _key));

            return this;
        }

        public IPublishSchemaDefinitionDescriptor IgnoreRootTypes()
        {
            _schemaDirectives.Add(new DirectiveNode("_removeRootTypes"));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor IgnoreType(
            NameString typeName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                "_removeType",
                new ArgumentNode("typeName", typeName.Value)));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor RenameType(
            NameString typeName,
            NameString newTypeName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                "_renameType",
                new ArgumentNode("typeName", typeName.Value),
                new ArgumentNode("newTypeName", newTypeName.Value)));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor RenameField(
            NameString typeName,
            NameString fieldName,
            NameString newFieldName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                "_renameField",
                new ArgumentNode("typeName", typeName.Value),
                new ArgumentNode("fieldName", fieldName.Value),
                new ArgumentNode("newFieldName", newFieldName.Value)));
            return this;
        }

        public RemoteSchemaDefinition Build(
            IDescriptorContext context,
            ISchema schema)
        {
            var extensionDocuments = new List<DocumentNode>(context.GetTypeExtensions(_key));

            if (_schemaDirectives.Count > 0)
            {
                var schemaExtension = new SchemaExtensionNode(
                    null,
                    _schemaDirectives,
                    Array.Empty<OperationTypeDefinitionNode>());

                extensionDocuments.Add(new DocumentNode(new[] { schemaExtension }));
            }

            return new RemoteSchemaDefinition(
                _name.HasValue ? _name : schema.Name,
                schema.ToDocument(),
                extensionDocuments);
        }
    }
}
