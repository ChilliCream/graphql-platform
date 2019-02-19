using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveFieldRewriter
        : ITypeRewriter
    {
        private readonly NameString? _schemaName;
        private readonly FieldReference _field;

        public RemoveFieldRewriter(FieldReference field)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public RemoveFieldRewriter(NameString schemaName, FieldReference field)
        {
            _schemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _field = field ?? throw new ArgumentNullException(nameof(field));
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            NameString typeName = typeDefinition.GetOriginalName(schema.Name);
            if (!_field.TypeName.Equals(typeName))
            {
                return typeDefinition;
            }

            switch (typeDefinition)
            {
                case InputObjectTypeDefinitionNode iotd:
                    return RemoveFields(iotd, schema.Name);

                case ObjectTypeDefinitionNode otd:
                    return RemoveFields(otd, schema.Name,
                        f => otd.WithFields(f));

                case InterfaceTypeDefinitionNode itd:
                    return RemoveFields(itd, schema.Name,
                        f => itd.WithFields(f));

                default:
                    return typeDefinition;
            }
        }

        private T RemoveFields<T>(
            T typeDefinition,
            NameString schemaName,
            RewriteFieldsDelegate<T> rewrite)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
        {
            var renamedFields = new List<FieldDefinitionNode>();

            foreach (FieldDefinitionNode field in typeDefinition.Fields)
            {
                if (!_field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(field);
                }
            }

            return rewrite(renamedFields);
        }

        private InputObjectTypeDefinitionNode RemoveFields(
            InputObjectTypeDefinitionNode typeDefinition,
            NameString schemaName)
        {
            var renamedFields = new List<InputValueDefinitionNode>();

            foreach (InputValueDefinitionNode field in typeDefinition.Fields)
            {
                if (!_field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(field);
                }
            }

            return typeDefinition.WithFields(renamedFields);
        }
    }
}
