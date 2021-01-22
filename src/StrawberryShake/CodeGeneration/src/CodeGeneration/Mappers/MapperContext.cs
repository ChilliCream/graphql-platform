using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public class MapperContext : IMapperContext
    {
        private readonly Dictionary<NameString, NamedTypeDescriptor> _types = new();
        private readonly Dictionary<NameString, EntityTypeDescriptor> _entityTypes = new();
        private readonly Dictionary<NameString, EnumDescriptor> _enums = new();
        private readonly Dictionary<NameString, OperationDescriptor> _operations = new();
        private readonly Dictionary<NameString, ResultBuilderDescriptor> _resultBuilder = new();
        private ClientDescriptor? _client = null;

        public MapperContext(string ns, string clientName)
        {
            Namespace = ns;
            ClientName = clientName;
        }

        public string ClientName { get; }

        public string Namespace { get; }

        public IReadOnlyCollection<NamedTypeDescriptor> Types => _types.Values;

        public IReadOnlyCollection<EntityTypeDescriptor> EntityTypes => _entityTypes.Values;

        public IReadOnlyCollection<EnumDescriptor> EnumTypes => _enums.Values;
        public IReadOnlyCollection<OperationDescriptor> Operations => _operations.Values;
        public IReadOnlyCollection<ResultBuilderDescriptor> ResultBuilders => _resultBuilder.Values;

        public ClientDescriptor Client => _client ?? throw new NotImplementedException();

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

            yield return Client;
        }

        public void Register(NameString codeTypeName, NamedTypeDescriptor typeDescriptor)
        {
            _types.Add(
                codeTypeName.EnsureNotEmpty(nameof(codeTypeName)),
                typeDescriptor ?? throw new ArgumentNullException(nameof(typeDescriptor)));
        }

        public void Register(NameString codeTypeName, EntityTypeDescriptor entityTypeDescriptor)
        {
            _entityTypes.Add(
                codeTypeName,
                entityTypeDescriptor);
        }

        public void Register(NameString codeTypeName, EnumDescriptor enumTypeDescriptor)
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
            throw new NotImplementedException();
        }
    }
}
