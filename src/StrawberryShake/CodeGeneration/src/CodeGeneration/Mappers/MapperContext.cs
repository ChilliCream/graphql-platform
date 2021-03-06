using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class MapperContext : IMapperContext
    {
        private readonly List<INamedTypeDescriptor> _types = new();
        private readonly List<EntityTypeDescriptor> _entityTypes = new();
        private readonly List<DataTypeDescriptor> _dataTypes = new();
        private readonly Dictionary<NameString, OperationDescriptor> _operations = new();
        private readonly Dictionary<NameString, ResultBuilderDescriptor> _resultBuilder = new();
        private ClientDescriptor? _client;
        private EntityIdFactoryDescriptor? _entityIdFactory;
        private DependencyInjectionDescriptor? _dependencyInjectionDescriptor;

        public MapperContext(
            string @namespace,
            string clientName,
            IDocumentHashProvider hashProvider,
            RequestStrategy requestStrategy,
            IReadOnlyList<TransportProfile> transportProfiles)
        {
            Namespace = @namespace;
            ClientName = clientName;
            HashProvider = hashProvider;
            RequestStrategy = requestStrategy;
            TransportProfiles = transportProfiles;
        }

        public string ClientName { get; }

        public string Namespace { get; }
        public string StateNamespace => Namespace + ".State";

        public RequestStrategy RequestStrategy { get; }

        public IDocumentHashProvider HashProvider { get; }

        public IReadOnlyList<TransportProfile> TransportProfiles { get; }

        public IReadOnlyList<INamedTypeDescriptor> Types => _types;

        public IReadOnlyCollection<DataTypeDescriptor> DataTypes => _dataTypes;

        public IReadOnlyCollection<EntityTypeDescriptor> EntityTypes => _entityTypes;

        public IReadOnlyCollection<OperationDescriptor> Operations => _operations.Values;

        public IReadOnlyCollection<ResultBuilderDescriptor> ResultBuilders => _resultBuilder.Values;

        public ClientDescriptor Client =>
            _client ?? throw new NotImplementedException();

        public EntityIdFactoryDescriptor EntityIdFactory =>
            _entityIdFactory ?? throw new NotImplementedException();

        public DependencyInjectionDescriptor DependencyInjection =>
            _dependencyInjectionDescriptor ?? throw new NotImplementedException();

        public IEnumerable<ICodeDescriptor> GetAllDescriptors()
        {
            foreach (var entityTypeDescriptor in EntityTypes)
            {
                yield return entityTypeDescriptor;
            }

            foreach (var type in Types)
            {
                yield return type;
            }

            foreach (var type in Operations)
            {
                yield return type;
            }

            foreach (var resultBuilder in ResultBuilders)
            {
                yield return resultBuilder;
            }

            foreach (var dataType in DataTypes)
            {
                yield return dataType;
            }

            yield return Client;

            yield return EntityIdFactory;

            yield return DependencyInjection;
        }

        public void Register(IEnumerable<INamedTypeDescriptor> typeDescriptors)
        {
            if (_types.Count > 0)
            {
                throw new InvalidOperationException(
                    "The types have already been registered.");
            }

            _types.AddRange(typeDescriptors);
        }

        public void Register(IEnumerable<DataTypeDescriptor> dataTypeDescriptors)
        {
            if (_dataTypes.Count > 0)
            {
                throw new InvalidOperationException(
                    "The data types have already been registered.");
            }

            _dataTypes.AddRange(dataTypeDescriptors);
        }

        public void Register(IEnumerable<EntityTypeDescriptor> entityTypeDescriptor)
        {
            if (_entityTypes.Count > 0)
            {
                throw new InvalidOperationException(
                    "The entity types have already been registered.");
            }

            _entityTypes.AddRange(entityTypeDescriptor);
        }

        public void Register(NameString operationName, OperationDescriptor operationDescriptor)
        {
            _operations.Add(
                operationName.EnsureNotEmpty(nameof(operationName)),
                operationDescriptor ??
                throw new ArgumentNullException(nameof(operationDescriptor)));
        }

        public void Register(
            NameString operationName,
            ResultBuilderDescriptor resultBuilderDescriptor)
        {
            _resultBuilder.Add(
                operationName.EnsureNotEmpty(nameof(operationName)),
                resultBuilderDescriptor ??
                throw new ArgumentNullException(nameof(resultBuilderDescriptor)));
        }

        public void Register(ClientDescriptor clientDescriptor)
        {
            _client = clientDescriptor;
        }

        public void Register(EntityIdFactoryDescriptor entityIdFactoryDescriptor)
        {
            _entityIdFactory = entityIdFactoryDescriptor;
        }

        public void Register(DependencyInjectionDescriptor dependencyInjectionDescriptor)
        {
            _dependencyInjectionDescriptor = dependencyInjectionDescriptor;
        }
    }
}
