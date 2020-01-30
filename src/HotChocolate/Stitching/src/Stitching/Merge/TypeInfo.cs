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
}
