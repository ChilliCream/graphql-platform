using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RenameFieldArgumentRewriter
        : ITypeRewriter
    {
        public RenameFieldArgumentRewriter(
            FieldReference field,
            NameString argumentName,
            NameString newArgumentName,
            NameString? schemaName = null)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            ArgumentName = argumentName.EnsureNotEmpty(nameof(argumentName));
            NewArgumentName = newArgumentName.EnsureNotEmpty(nameof(newArgumentName));
            SchemaName = schemaName?.EnsureNotEmpty(nameof(schemaName));
        }

        public FieldReference Field { get; }

        public NameString ArgumentName { get; }

        public NameString NewArgumentName { get; }

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
                case ObjectTypeDefinitionNode otd:
                    return SelectField(otd, schema.Name,
                        f => otd.WithFields(f));

                case InterfaceTypeDefinitionNode itd:
                    return SelectField(itd, schema.Name,
                        f => itd.WithFields(f));

                default:
                    return typeDefinition;
            }
        }

        private T SelectField<T>(
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
                    renamedFields.Add(RewriteArgument(schemaName, field));
                }
                else
                {
                    renamedFields.Add(field);
                }
            }

            return rewrite(renamedFields);
        }

        private FieldDefinitionNode RewriteArgument(
            NameString schemaName,
            FieldDefinitionNode field)
        {
            var renamedArguments = new List<InputValueDefinitionNode>();

            foreach (InputValueDefinitionNode argument in field.Arguments)
            {
                if (ArgumentName.Equals(argument.Name.Value))
                {
                    renamedArguments.Add(argument.Rename(
                        NewArgumentName, schemaName));
                }
                else
                {
                    renamedArguments.Add(argument);
                }
            }

            return field.WithArguments(renamedArguments);
        }
    }
}
