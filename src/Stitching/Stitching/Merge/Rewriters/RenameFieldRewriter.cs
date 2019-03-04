using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RenameFieldRewriter
        : ITypeRewriter
    {
        public RenameFieldRewriter(
            FieldReference field,
            NameString newFieldName)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            NewFieldName = newFieldName.EnsureNotEmpty(nameof(newFieldName));
        }

        public RenameFieldRewriter(
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            NewFieldName = newFieldName.EnsureNotEmpty(nameof(newFieldName));
        }

        public NameString? SchemaName { get; }

        public FieldReference Field { get; }

        public NameString NewFieldName { get; }

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
                    return RenameFields(iotd, schema.Name);

                case ObjectTypeDefinitionNode otd:
                    return RenameFields(otd, schema.Name,
                        f => otd.WithFields(f));

                case InterfaceTypeDefinitionNode itd:
                    return RenameFields(itd, schema.Name,
                        f => itd.WithFields(f));

                default:
                    return typeDefinition;
            }
        }

        private T RenameFields<T>(
            T typeDefinition,
            NameString schemaName,
            RewriteFieldsDelegate<T> rewrite)
            where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
        {
            var renamedFields = new List<FieldDefinitionNode>();

            foreach (FieldDefinitionNode field in typeDefinition.Fields)
            {
                if (Field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(
                        field.Rename(NewFieldName, schemaName));
                }
                else
                {
                    renamedFields.Add(field);
                }
            }

            return rewrite(renamedFields);
        }

        private InputObjectTypeDefinitionNode RenameFields(
            InputObjectTypeDefinitionNode typeDefinition,
            NameString schemaName)
        {
            var renamedFields = new List<InputValueDefinitionNode>();

            foreach (InputValueDefinitionNode field in typeDefinition.Fields)
            {
                if (Field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(
                        field.Rename(NewFieldName, schemaName));
                }
                else
                {
                    renamedFields.Add(field);
                }
            }

            return typeDefinition.WithFields(renamedFields);
        }
    }
}
