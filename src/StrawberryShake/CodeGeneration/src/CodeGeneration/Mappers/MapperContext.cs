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

        public MapperContext(string ns)
        {
            Namespace = ns;
        }

        public string Namespace { get; }

        public IReadOnlyCollection<NamedTypeDescriptor> Types => _types.Values;

        public IReadOnlyCollection<EntityTypeDescriptor> EntityTypes => _entityTypes.Values;

        public IReadOnlyCollection<EnumDescriptor> EnumTypes => _enums.Values;

        public IEnumerable<ICodeDescriptor> GetAllDescriptors()
        {
            foreach (var entityTypeDescriptor in EntityTypes)
            {
                yield return entityTypeDescriptor;
            }
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
    }
}
