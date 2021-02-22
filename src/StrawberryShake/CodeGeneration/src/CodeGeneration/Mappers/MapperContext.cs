using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class MapperContext : IMapperContext
    {
        private readonly List<INamedTypeDescriptor> _types = new();
        private readonly Dictionary<NameString, EntityTypeDescriptor> _entityTypes = new();
        private readonly Dictionary<NameString, DataTypeDescriptor> _dataTypes = new();
        private readonly Dictionary<NameString, EnumTypeDescriptor> _enums = new();
        private readonly Dictionary<NameString, OperationDescriptor> _operations = new();
        private readonly Dictionary<NameString, ResultBuilderDescriptor> _resultBuilder = new();
        private ClientDescriptor? _client;
        private EntityIdFactoryDescriptor? _entityIdFactory;
        private DependencyInjectionDescriptor? _dependencyInjectionDescriptor;

        public MapperContext(string ns, string clientName)
        {
            Namespace = ns;
            ClientName = clientName;
        }

        public string ClientName { get; }

        public string Namespace { get; }
        public string StateNamespace => Namespace + ".State";

        public IReadOnlyCollection<NamedTypeDescriptor> Types => _types;

        public IReadOnlyCollection<EntityTypeDescriptor> EntityTypes => _entityTypes.Values;
        public IReadOnlyCollection<DataTypeDescriptor> DataTypes => _dataTypes.Values;

        public IReadOnlyCollection<EnumTypeDescriptor> EnumTypes => _enums.Values;

        public IReadOnlyCollection<OperationDescriptor> Operations => _operations.Values;

        public IReadOnlyCollection<ResultBuilderDescriptor> ResultBuilders => _resultBuilder.Values;

        public ClientDescriptor Client =>
            _client ?? throw new NotImplementedException();

        public EntityIdFactoryDescriptor EntityIdFactory =>
            _entityIdFactory ?? throw new NotImplementedException();

        public DependencyInjectionDescriptor DependencyInjection =>
            _dependencyInjectionDescriptor ?? throw new NotImplementedException();

        IReadOnlyCollection<INamedTypeDescriptor> IMapperContext.Types => throw new NotImplementedException();

        public IEnumerable<ICodeDescriptor> GetAllDescriptors()
        {
            foreach (var entityTypeDescriptor in EntityTypes)
            {
                yield return entityTypeDescriptor;
            }

            foreach (var enumTypeDescriptor in EnumTypes)
            {
                yield return enumTypeDescriptor;
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
                throw new InvalidOperationException("The types have already been registered.");
            }

            _types.AddRange(typeDescriptors);
        }

        public void Register(NameString codeTypeName, EntityTypeDescriptor entityTypeDescriptor)
        {
            _entityTypes.Add(
                codeTypeName,
                entityTypeDescriptor);
        }

        public void Register(NameString codeTypeName, DataTypeDescriptor entityTypeDescriptor)
        {
            _dataTypes.Add(
                codeTypeName,
                entityTypeDescriptor);
        }

        public void Register(NameString codeTypeName, EnumTypeDescriptor enumTypeDescriptor)
        {
            _enums.Add(
                codeTypeName.EnsureNotEmpty(nameof(codeTypeName)),
                enumTypeDescriptor ?? throw new ArgumentNullException(nameof(enumTypeDescriptor)));
        }

        public void Register(NameString operationName, OperationDescriptor operationDescriptor)
        {
            _operations.Add(
                operationName.EnsureNotEmpty(nameof(operationName)),
                operationDescriptor ??
                throw new ArgumentNullException(nameof(operationDescriptor)));
        }

        public void Register(NameString operationName, ResultBuilderDescriptor resultBuilderDescriptor)
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
