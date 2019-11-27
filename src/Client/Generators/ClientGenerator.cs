using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using HotChocolate.Validation;
using HotChocolate.Types;
using HotChocolate.Stitching;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.CSharp;
using StrawberryShake.Generators.Types;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Generators
{
    public class ClientGenerator
    {
        private readonly Dictionary<string, DocumentNode> _schemas =
            new Dictionary<string, DocumentNode>();
        private readonly List<DocumentNode> _extensions =
            new List<DocumentNode>();
        private readonly Dictionary<string, DocumentInfo> _queries =
            new Dictionary<string, DocumentInfo>();
        private readonly Dictionary<string, LeafTypeInfo> _leafTypes =
            new[]
            {
                new LeafTypeInfo(ScalarNames.String, typeof(string)),
                new LeafTypeInfo(ScalarNames.Int, typeof(int)),
                new LeafTypeInfo(ScalarNames.Float, typeof(double)),
                new LeafTypeInfo(ScalarNames.Boolean, typeof(bool)),
                new LeafTypeInfo(ScalarNames.ID, typeof(string)),
                new LeafTypeInfo(ScalarNames.Date, typeof(DateTime), typeof(string)),
                new LeafTypeInfo(ScalarNames.DateTime, typeof(DateTimeOffset), typeof(string)),
                new LeafTypeInfo(ScalarNames.Byte, typeof(byte) , typeof(byte)),
                new LeafTypeInfo(ScalarNames.Short, typeof(short)),
                new LeafTypeInfo(ScalarNames.Long, typeof(long)),
                new LeafTypeInfo(ScalarNames.Decimal, typeof(decimal), typeof(decimal)),
                new LeafTypeInfo(ScalarNames.Uuid, typeof(Guid), typeof(string)),
                new LeafTypeInfo("Guid", typeof(Guid), typeof(string)),
                new LeafTypeInfo(ScalarNames.Url, typeof(Uri), typeof(string))
            }.ToDictionary(t => t.TypeName);

        private readonly ClientGeneratorOptions _options = new ClientGeneratorOptions();
        private IDocumentHashProvider? _hashProvider;
        private IFileHandler? _output;
        private string? _clientName;
        private string? _namespace;

        private ClientGenerator()
        {
        }

        public static ClientGenerator New() => new ClientGenerator();

        public ClientGenerator SetOutput(string directoryName)
        {
            if (directoryName is null)
            {
                throw new ArgumentNullException(nameof(directoryName));
            }

            return SetOutput(new DirectoryFileHandler(directoryName));
        }

        public ClientGenerator SetOutput(IFileHandler output)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            _output = output;
            return this;
        }

        public ClientGenerator ModifyOptions(
            Action<ClientGeneratorOptions> modify)
        {
            if (modify is null)
            {
                throw new ArgumentNullException(nameof(modify));
            }

            modify(_options);
            return this;
        }

        public ClientGenerator SetHashProvider(
            IDocumentHashProvider hashProvider)
        {
            _hashProvider = hashProvider;
            return this;
        }

        public ClientGenerator SetScalarType(LeafTypeInfo typeInfo)
        {
            if (typeInfo is null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            _leafTypes[typeInfo.TypeName] = typeInfo;
            return this;
        }

        public ClientGenerator AddSchemaDocumentFromFile(string fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return AddSchemaDocument(
                IOPath.GetFileNameWithoutExtension(fileName),
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(fileName)));
        }

        public ClientGenerator AddSchemaDocumentFromString(
            string name, string schema)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return AddSchemaDocument(
                name,
                Utf8GraphQLParser.Parse(schema));
        }

        public ClientGenerator AddSchemaDocument(
            string name,
            DocumentNode document)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (_clientName is null)
            {
                _clientName = name + "Client";
            }

            var extensions = new HashSet<IDefinitionNode>(
                document.Definitions.OfType<ITypeExtensionNode>());

            if (extensions.Count == 0)
            {
                _schemas.Add(name, document);
                return this;
            }

            var types = extensions.Except(document.Definitions).ToList();

            if (types.Count > 0)
            {
                _schemas.Add(name, document.WithDefinitions(types));
            }

            _extensions.Add(document.WithDefinitions(extensions.ToList()));

            return this;
        }

        public ClientGenerator AddQueryDocumentFromFile(string fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            string name = IOPath.GetFileNameWithoutExtension(fileName);
            DocumentNode document = Utf8GraphQLParser.Parse(File.ReadAllBytes(fileName));
            _queries.Add(name, new DocumentInfo(name, fileName, document));
            return this;
        }

        public ClientGenerator AddQueryDocumentFromString(
            string name, string query)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return AddQueryDocument(
                name,
                Utf8GraphQLParser.Parse(query));
        }

        public ClientGenerator AddQueryDocument(
            string name,
            DocumentNode document) =>
            AddQueryDocument(name, name, document);

        public ClientGenerator AddQueryDocument(
            string name,
            string fileName,
            DocumentNode document)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _queries.Add(name, new DocumentInfo(name, fileName, document));
            return this;
        }

        public ClientGenerator SetClientName(string clientName)
        {
            _clientName = clientName
                ?? throw new ArgumentNullException(nameof(clientName));
            return this;
        }

        public ClientGenerator SetNamespace(string ns)
        {
            _namespace = ns
                ?? throw new ArgumentNullException(nameof(ns));
            return this;
        }

        public IReadOnlyList<HCError> Validate()
        {
            if (_output is null)
            {
                throw new InvalidOperationException(
                    "You have to specify a field output handler before you " +
                    "can generate any client APIs.");
            }

            if (_schemas.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one schema file before you " +
                    "can generate any client APIs.");
            }

            if (_queries.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one query file before you " +
                    "can generate any client APIs.");
            }

            // create schema
            DocumentNode mergedSchema = MergeSchema();
            mergedSchema = MergeSchemaExtensions(mergedSchema);
            ISchema schema = CreateSchema(mergedSchema);

            // parse queries
            return ValidateQueryDocuments(schema);
        }

        public async Task BuildAsync()
        {
            if (_output is null)
            {
                throw new InvalidOperationException(
                    "You have to specify a field output handler before you " +
                    "can generate any client APIs.");
            }

            if (_schemas.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one schema file before you " +
                    "can generate any client APIs.");
            }

            if (_queries.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one query file before you " +
                    "can generate any client APIs.");
            }

            IDocumentHashProvider hashProvider = _hashProvider
                ?? new MD5DocumentHashProvider();
            _namespace = _namespace ?? "StrawberryShake.Client";

            // create schema
            DocumentNode mergedSchema = MergeSchema();
            mergedSchema = MergeSchemaExtensions(mergedSchema);
            ISchema schema = CreateSchema(mergedSchema);
            InitializeScalarTypes(schema);

            // parse queries
            IReadOnlyList<HCError> errors = ValidateQueryDocuments(schema);
            if (errors.Count > 0)
            {
                throw new GeneratorException(errors);
            }

            IReadOnlyList<IQueryDescriptor> queries =
                await ParseQueriesAsync(hashProvider)
                    .ConfigureAwait(false);

            // generate abstarct client models
            var usedNames = new HashSet<string>();
            var descriptors = new List<ICodeDescriptor>();
            var fieldTypes = new Dictionary<FieldNode, string>();

            GenerateModels(schema, queries, usedNames, descriptors, fieldTypes);

            var typeLookup = new TypeLookup(
                _options.LanguageVersion,
                _leafTypes.Values,
                fieldTypes);

            // generate code from models
            foreach (ICodeGenerator generator in CreateGenerators(_options))
            {
                foreach (ICodeDescriptor descriptor in descriptors)
                {
                    if (generator.CanHandle(descriptor))
                    {
                        _output.Register(descriptor, generator);
                    }
                }
            }

            await _output.WriteAllAsync(typeLookup)
                .ConfigureAwait(false);
        }

        private DocumentNode MergeSchema()
        {
            if (_schemas.Count == 1)
            {
                return _schemas.First().Value;
            }

            SchemaMerger merger = SchemaMerger.New();

            foreach (KeyValuePair<string, DocumentNode> schema in _schemas)
            {
                merger.AddSchema(schema.Key, schema.Value);
            }

            return merger.Merge();
        }

        private DocumentNode MergeSchemaExtensions(DocumentNode schema)
        {
            if (_extensions.Count == 0)
            {
                return schema;
            }

            var rewriter = new AddSchemaExtensionRewriter(new[]
            {
                new DirectiveDefinitionNode
                (
                    null,
                    new NameNode(GeneratorDirectives.ClrType),
                    null,
                    false,
                    new[]
                    {
                        new InputValueDefinitionNode(
                            null,
                            new NameNode(GeneratorDirectives.NameArgument),
                            null,
                            new NonNullTypeNode(new NamedTypeNode(ScalarNames.String)),
                            null,
                            Array.Empty<DirectiveNode>()
                        )
                    },
                    new []
                    {
                        new NameNode(HotChocolate.Language.DirectiveLocation.Scalar.ToString())
                    }
                ),
                new DirectiveDefinitionNode
                (
                    null,
                    new NameNode(GeneratorDirectives.SerializationType),
                    null,
                    false,
                    new[]
                    {
                        new InputValueDefinitionNode(
                            null,
                            new NameNode(GeneratorDirectives.NameArgument),
                            null,
                            new NonNullTypeNode(new NamedTypeNode(ScalarNames.String)),
                            null,
                            Array.Empty<DirectiveNode>()
                        )
                    },
                    new []
                    {
                        new NameNode(HotChocolate.Language.DirectiveLocation.Scalar.ToString())
                    }
                ),
                new DirectiveDefinitionNode
                (
                    null,
                    new NameNode(GeneratorDirectives.Name),
                    null,
                    false,
                    new[]
                    {
                        new InputValueDefinitionNode(
                            null,
                            new NameNode(GeneratorDirectives.NameArgument),
                            null,
                            new NonNullTypeNode(new NamedTypeNode(ScalarNames.String)),
                            null,
                            Array.Empty<DirectiveNode>()
                        )
                    },
                    new []
                    {
                        new NameNode(HotChocolate.Language.DirectiveLocation.Scalar.ToString())
                    }
                )
            });

            DocumentNode currentSchema = schema;
            foreach (DocumentNode extension in _extensions)
            {
                currentSchema = rewriter.AddExtensions(
                    currentSchema, extension);
            }

            return currentSchema;
        }

        private static ISchema CreateSchema(DocumentNode schema)
        {
            var builder = SchemaBuilder.New();

            foreach (CustomScalarType type in schema.Definitions
                .OfType<ScalarTypeDefinitionNode>()
                .Where(t => !Scalars.IsBuiltIn(t.Name.Value))
                .Select(t => new CustomScalarType(t)))
            {
                builder.AddType(type);
            }

            return builder.Use(next => context => Task.CompletedTask)
                .AddDocument(schema)
                .AddDirectiveType<NameDirectiveType>()
                .AddDirectiveType<TypeDirectiveType>()
                .AddDirectiveType<SerializationDirectiveType>()
                .AddDirectiveType<DelegateDirectiveType>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();
        }

        private async Task<IReadOnlyList<IQueryDescriptor>> ParseQueriesAsync(
            IDocumentHashProvider hashProvider)
        {
            var queryCollection = new QueryCollection(hashProvider, _namespace!);

            foreach (DocumentInfo documentInfo in _queries.Values)
            {
                await queryCollection.LoadFromDocumentAsync(
                    documentInfo.Name,
                    documentInfo.FileName,
                    documentInfo.Document)
                    .ConfigureAwait(false);
            }

            return queryCollection.ToList();
        }

        private void GenerateModels(
            ISchema schema,
            IEnumerable<IQueryDescriptor> queries,
            ISet<string> usedNames,
            List<ICodeDescriptor> descriptors,
            Dictionary<FieldNode, string> fieldTypes)
        {
            foreach (IQueryDescriptor query in queries)
            {
                var modelGenerator = new CodeModelGenerator(
                    schema, query, usedNames, _clientName!, _namespace!);
                modelGenerator.Generate();

                descriptors.AddRange(modelGenerator.Descriptors);

                foreach (KeyValuePair<FieldNode, string> fieldType in
                    modelGenerator.FieldTypes)
                {
                    fieldTypes[fieldType.Key] = fieldType.Value;
                }
            }
        }
        // new LeafTypeInfo("DateTime", typeof(DateTimeOffset), typeof(string)),

        private void InitializeScalarTypes(ISchema schema)
        {
            foreach (CustomScalarType scalarType in schema.Types.OfType<CustomScalarType>())
            {
                if (!_leafTypes.TryGetValue(scalarType.Name, out LeafTypeInfo? typeInfo))
                {
                    Type? clrType = GetTypeFromDirective(
                        scalarType,
                        GeneratorDirectives.ClrType);

                    if (clrType is null)
                    {
                        clrType = typeof(string);
                    }

                    Type serializationType = GetTypeFromDirective(
                        scalarType,
                        GeneratorDirectives.SerializationType) ??
                        clrType;

                    _leafTypes[scalarType.Name] =
                        new LeafTypeInfo(scalarType.Name, clrType, serializationType);
                }
            }
        }

        private static Type? GetTypeFromDirective(
            CustomScalarType scalarType,
            string directiveName)
        {
            DirectiveNode typeDirective = scalarType.SyntaxNode.Directives
                .FirstOrDefault(t => t.Name.Value.Equals(directiveName));

            if (typeDirective is null)
            {
                return null;
            }

            ArgumentNode name = typeDirective.Arguments.First(t =>
                t.Name.Value.Equals(GeneratorDirectives.Name));

            return Type.GetType(((StringValueNode)name.Value).Value);
        }

        private IReadOnlyList<HCError> ValidateQueryDocuments(ISchema schema)
        {
            var errors = new List<HCError>();

            try
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddQueryValidation();
                serviceCollection.AddDefaultValidationRules();
                serviceCollection.AddSingleton<IValidateQueryOptionsAccessor, ValidationOptions>();
                IQueryValidator validator = serviceCollection.BuildServiceProvider()
                    .GetService<IQueryValidator>();

                foreach (DocumentInfo documentInfo in _queries.Values)
                {
                    QueryValidationResult validationResult =
                        validator.Validate(schema, documentInfo.Document);

                    if (validationResult.HasErrors)
                    {
                        foreach (HCError error in validationResult.Errors)
                        {
                            errors.Add(HCErrorBuilder.FromError(error)
                                .SetExtension("fileName", documentInfo.FileName)
                                .SetExtension("document", documentInfo.Document)
                                .Build());
                        }
                    }
                }
            }
            catch (GeneratorException ex)
            {
                errors.AddRange(ex.Errors);
            }

            return errors;
        }

        private static IEnumerable<ICodeGenerator> CreateGenerators(
            ClientGeneratorOptions options)
        {
            yield return new ClassGenerator();
            yield return new InputClassGenerator();
            yield return new InputClassSerializerGenerator(options.LanguageVersion);
            yield return new InterfaceGenerator();
            yield return new ResultParserGenerator(options);
            yield return new OperationGenerator();
            yield return new ClientInterfaceGenerator();
            yield return new ClientClassGenerator();
            yield return new QueryGenerator();
            yield return new EnumGenerator();
            yield return new EnumValueSerializerGenerator(options.LanguageVersion);

            if (options.EnableDISupport)
            {
                yield return new ServicesGenerator();
            }
        }

        private class DocumentInfo
        {
            public DocumentInfo(
                string name,
                string fileName,
                DocumentNode document)
            {
                Name = name;
                FileName = fileName;
                Document = document;
            }

            public string Name { get; }

            public string FileName { get; }

            public DocumentNode Document { get; }
        }

        private class ValidationOptions
            : IValidateQueryOptionsAccessor
        {
            public int? MaxExecutionDepth => null;

            public int? MaxOperationComplexity => null;

            public bool? UseComplexityMultipliers => null;
        }

        internal class ScalarTypeMergeHandler
            : ITypeMergeHandler
        {
            private readonly MergeTypeRuleDelegate _next;

            public ScalarTypeMergeHandler(MergeTypeRuleDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public void Merge(
                ISchemaMergeContext context,
                IReadOnlyList<HotChocolate.Stitching.Merge.ITypeInfo> types)
            {
                if (types.OfType<ITypeInfo<ScalarTypeDefinitionNode>>().Any())
                {
                    ITypeInfo<ScalarTypeDefinitionNode> scalar =
                        types.OfType<ITypeInfo<ScalarTypeDefinitionNode>>().FirstOrDefault();
                    context.AddType(scalar.Definition);
                    return;
                }
                _next(context, types);
            }
        }
    }
}
