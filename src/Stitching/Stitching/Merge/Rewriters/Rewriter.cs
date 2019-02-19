using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal delegate T RewriteFieldsDelegate<T>(
        IReadOnlyList<FieldDefinitionNode> fields)
        where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode;

    public interface ITypeRewriter
    {
        ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition);
    }


    internal class RenameTypeRewriter
        : ITypeRewriter
    {
        private readonly NameString? _schemaName;
        private readonly NameString _originalTypeName;
        private readonly NameString _newTypeName;

        public RenameTypeRewriter(
            NameString originalTypeName,
            NameString newTypeName)
        {
            _originalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            _newTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
        }

        public RenameTypeRewriter(
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            _schemaName = schemaName
                .EnsureNotEmpty(nameof(schemaName));
            _originalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            _newTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            if (!_originalTypeName.Equals(typeDefinition.Name.Value))
            {
                return typeDefinition;
            }

            return typeDefinition.AddSource(_newTypeName, schema.Name);
        }
    }

    internal class RenameFieldRewriter
        : ITypeRewriter
    {
        private readonly NameString? _schemaName;
        private readonly FieldReference _field;
        private readonly NameString _newFieldName;

        public RenameFieldRewriter(
            FieldReference field,
            NameString newFieldName)
        {
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _newFieldName = newFieldName.EnsureNotEmpty(nameof(newFieldName));
        }

        public RenameFieldRewriter(
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            _schemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _newFieldName = newFieldName.EnsureNotEmpty(nameof(newFieldName));
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            if (!_field.TypeName.Equals(typeDefinition.Name.Value))
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
                if (_field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(
                        field.AddSource(_newFieldName, schemaName));
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
                if (_field.FieldName.Equals(field.Name.Value))
                {
                    renamedFields.Add(
                        field.AddSource(_newFieldName, schemaName));
                }
                else
                {
                    renamedFields.Add(field);
                }
            }

            return typeDefinition.WithFields(renamedFields);
        }
    }

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

            if (!_field.TypeName.Equals(typeDefinition.Name.Value))
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
