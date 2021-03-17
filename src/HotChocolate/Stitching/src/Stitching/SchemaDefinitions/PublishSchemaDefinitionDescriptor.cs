using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.SchemaDefinitions
{
    public class PublishSchemaDefinitionDescriptor : IPublishSchemaDefinitionDescriptor
    {
        private readonly IRequestExecutorBuilder _builder;
        private readonly string _key = Guid.NewGuid().ToString();
        private readonly List<DirectiveNode> _schemaDirectives = new List<DirectiveNode>();
        private Func<IServiceProvider,  ISchemaDefinitionPublisher>? _publisherFactory;
        private NameString _name;
        private RemoteSchemaDefinition? _schemaDefinition;

        public PublishSchemaDefinitionDescriptor(IRequestExecutorBuilder builder)
        {
            _builder = builder;
        }

        public bool HasPublisher => _publisherFactory is not null;

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
                        throw ThrowHelper.PublishSchemaDefinitionDescriptor_ResourceNotFound(key);
                    }

#if NET5_0 || NET6_0
                    await using (stream)
#else
                    using (stream)
#endif
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
                s =>
                {
                    s.AddTypeExtensions(Utf8GraphQLParser.Parse(schemaSdl), _key);
                });

            return this;
        }

        public IPublishSchemaDefinitionDescriptor SetSchemaDefinitionPublisher(
            Func<IServiceProvider, ISchemaDefinitionPublisher> publisherFactory)
        {
            _publisherFactory = publisherFactory;
            return this;
        }

        public IPublishSchemaDefinitionDescriptor IgnoreRootTypes()
        {
            _schemaDirectives.Add(new DirectiveNode(DirectiveNames.RemoveRootTypes));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor IgnoreType(
            NameString typeName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                DirectiveNames.RemoveType,
                new ArgumentNode(DirectiveFieldNames.RemoveType_TypeName, typeName)));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor RenameType(
            NameString typeName,
            NameString newTypeName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                DirectiveNames.RenameType,
                new ArgumentNode(DirectiveFieldNames.RenameType_TypeName, typeName),
                new ArgumentNode(DirectiveFieldNames.RenameType_NewTypeName, newTypeName)));
            return this;
        }

        public IPublishSchemaDefinitionDescriptor RenameField(
            NameString typeName,
            NameString fieldName,
            NameString newFieldName)
        {
            _schemaDirectives.Add(new DirectiveNode(
                DirectiveNames.RenameField,
                new ArgumentNode(DirectiveFieldNames.RenameField_TypeName, typeName),
                new ArgumentNode(DirectiveFieldNames.RenameField_FieldName, fieldName),
                new ArgumentNode(DirectiveFieldNames.RenameField_NewFieldName, newFieldName)));
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

            _schemaDefinition = new RemoteSchemaDefinition(
                _name.HasValue ? _name : schema.Name,
                schema.ToDocument(),
                extensionDocuments);

            return _schemaDefinition;
        }

        public async ValueTask PublishAsync(
            IServiceProvider applicationServices,
            CancellationToken cancellationToken = default)
        {
            if (_publisherFactory is not null &&
                _schemaDefinition is not null)
            {
                ISchemaDefinitionPublisher publisher = _publisherFactory(applicationServices);
                await publisher.PublishAsync(_schemaDefinition, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
