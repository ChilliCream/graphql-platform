using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeus.Abstractions;

namespace Zeus.Abstractions
{
    public class SchemaDocument
        : ISchemaDocument
    {
        private string _stringRepresentation;

        public SchemaDocument(IEnumerable<ITypeDefinition> typeDefinitions)
        {
            if (typeDefinitions == null)
            {
                throw new ArgumentNullException(nameof(typeDefinitions));
            }

            InterfaceTypes = typeDefinitions
                .OfType<InterfaceTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            ObjectTypes = typeDefinitions
                .OfType<ObjectTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            InputObjectTypes = typeDefinitions
                .OfType<InputObjectTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            UnionTypes = typeDefinitions
                .OfType<UnionTypeDefinition>()
                .ToDictionary(t => t.Name, StringComparer.Ordinal);

            EnumTypes = typeDefinitions
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