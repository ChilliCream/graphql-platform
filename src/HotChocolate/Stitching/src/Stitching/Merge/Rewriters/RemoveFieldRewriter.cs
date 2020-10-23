using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveFieldRewriter
        : ITypeRewriter
    {
        public RemoveFieldRewriter(FieldReference field, NameString? schemaName = null)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            SchemaName = schemaName?.EnsureNotEmpty(nameof(schemaName));
        }

        public FieldReference Field { get; }

        public NameString? SchemaName { get; }
        
        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (SchemaName.HasValue && !SchemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            NameString typeName = typeDefinition.GetOriginalName(schema.Name);
            if (!Field.TypeName.Equals(typeName))
            {
                return typeDefinition;
            }

            switch (typeDefinition)
            {
                case InputObjectTypeDefinitionNode iotd:
                    return RemoveFields(iotd);

                case ObjectTypeDefinitionNode otd:
                    return RemoveFields(otd, f => otd.WithFields(f));

                case InterfaceTypeDefinitionNode itd:
                    return RemoveFields(itd, f => itd.WithFields(f));

                default:
                    return typeDefinition;
            }
        }

        private T RemoveFields<T>(
            T typeDefinition,
            RewriteFieldsDelegate<T> rewrite)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
        {
            var renamedFields = new List<FieldDefinitionNode>();

            foreach (FieldDefinitionNode field in typeDefinition.Fields)
            {
                if (!Field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(field);
                }
            }

            return rewrite(renamedFields);
        }

        private InputObjectTypeDefinitionNode RemoveFields(
            InputObjectTypeDefinitionNode typeDefinition)
        {
            var renamedFields = new List<InputValueDefinitionNode>();

            foreach (InputValueDefinitionNode field in typeDefinition.Fields)
            {
                if (!Field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(field);
                }
            }

            return typeDefinition.WithFields(renamedFields);
        }
    }
}
