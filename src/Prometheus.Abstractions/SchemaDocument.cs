using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prometheus.Abstractions;

namespace Prometheus.Abstractions
{
    public class SchemaDocument
        : ISchemaDocument
    {
        private readonly ITypeDefinition[] _typeDefinitions;

        private string _stringRepresentation;

        public SchemaDocument(IEnumerable<ITypeDefinition> typeDefinitions)
        {
            if (typeDefinitions == null)
            {
                throw new ArgumentNullException(nameof(typeDefinitions));
            }

            _typeDefinitions = typeDefinitions.ToArray();

            InterfaceTypes = _typeDefinitions
                .OfType<InterfaceTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            ObjectTypes = _typeDefinitions
                .OfType<ObjectTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            InputObjectTypes = _typeDefinitions
                .OfType<InputObjectTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            UnionTypes = _typeDefinitions
                .OfType<UnionTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            EnumTypes = _typeDefinitions
                .OfType<EnumTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            if (ObjectTypes.TryGetValue(WellKnownTypes.Query, out var queryType))
            {
                QueryType = queryType;
            }

            if (ObjectTypes.TryGetValue(WellKnownTypes.Mutation, out var mutationType))
            {
                MutationType = mutationType;
            }
        }

        public IReadOnlyDictionary<string, InterfaceTypeDefinition> InterfaceTypes { get; }

        public IReadOnlyDictionary<string, EnumTypeDefinition> EnumTypes { get; }

        public IReadOnlyDictionary<string, ObjectTypeDefinition> ObjectTypes { get; }

        public IReadOnlyDictionary<string, UnionTypeDefinition> UnionTypes { get; }

        public IReadOnlyDictionary<string, InputObjectTypeDefinition> InputObjectTypes { get; }

        public ObjectTypeDefinition QueryType { get; }

        public ObjectTypeDefinition MutationType { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                StringBuilder sb = new StringBuilder();

                WriteTypeDefinitions(sb, InterfaceTypes.Values);
                WriteTypeDefinitions(sb, EnumTypes.Values);
                WriteTypeDefinitions(sb, ObjectTypes.Values);
                WriteTypeDefinitions(sb, UnionTypes.Values);
                WriteTypeDefinitions(sb, InputObjectTypes.Values);

                _stringRepresentation = sb.ToString();
            }

            return _stringRepresentation;
        }

        #region GetEnumerator

        public IEnumerator<ITypeDefinition> GetEnumerator()
        {
            return _typeDefinitions.OfType<ITypeDefinition>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private void WriteTypeDefinitions(StringBuilder sb, IEnumerable<ITypeDefinition> typeDefinitions)
        {
            if (typeDefinitions.Any())
            {
                sb.AppendLine(string.Join("\r\n\r\n", typeDefinitions.Select(t => t.ToString())));
                sb.AppendLine("");
            }
        }
    }
}