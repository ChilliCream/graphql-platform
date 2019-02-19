using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal class TypeInfo
        : ITypeInfo
    {
        protected TypeInfo(
            ITypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
        {
            Definition = typeDefinition
                ?? throw new ArgumentNullException(nameof(typeDefinition));
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            IsRootType = schema.IsRootType(typeDefinition);
        }

        public ITypeDefinitionNode Definition { get; }

        public ISchemaInfo Schema { get; }

        public bool IsRootType { get; }

        public static ITypeInfo Create(
            ITypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
        {
            switch (typeDefinition)
            {
                case ObjectTypeDefinitionNode otd:
                    return new ObjectTypeInfo(otd, schema);
                case InterfaceTypeDefinitionNode itd:
                    return new InterfaceTypeInfo(itd, schema);
                case UnionTypeDefinitionNode utd:
                    return new UnionTypeInfo(utd, schema);
                case InputObjectTypeDefinitionNode iotd:
                    return new InputObjectTypeInfo(iotd, schema);
                case EnumTypeDefinitionNode etd:
                    return new EnumTypeInfo(etd, schema);
                case ScalarTypeDefinitionNode std:
                    return new ScalarTypeInfo(std, schema);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    internal class TypeInfo<T>
        : TypeInfo
        , ITypeInfo<T>
        where T : ITypeDefinitionNode
    {
        protected TypeInfo(T typeDefinition, ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
            Definition = typeDefinition;
        }

        public new T Definition { get; }
    }

    internal class ObjectTypeInfo
        : TypeInfo<ObjectTypeDefinitionNode>
    {
        public ObjectTypeInfo(
            ObjectTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }

    internal class InterfaceTypeInfo
        : TypeInfo<InterfaceTypeDefinitionNode>
    {
        public InterfaceTypeInfo(
            InterfaceTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }

    internal class UnionTypeInfo
        : TypeInfo<UnionTypeDefinitionNode>
    {
        public UnionTypeInfo(
            UnionTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }

    internal class InputObjectTypeInfo
        : TypeInfo<InputObjectTypeDefinitionNode>
    {
        public InputObjectTypeInfo(
            InputObjectTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }

    internal class EnumTypeInfo
        : TypeInfo<EnumTypeDefinitionNode>
    {
        public EnumTypeInfo(
            EnumTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }

    internal class ScalarTypeInfo
        : TypeInfo<ScalarTypeDefinitionNode>
    {
        public ScalarTypeInfo(
            ScalarTypeDefinitionNode typeDefinition,
            ISchemaInfo schema)
            : base(typeDefinition, schema)
        {
        }
    }
}
