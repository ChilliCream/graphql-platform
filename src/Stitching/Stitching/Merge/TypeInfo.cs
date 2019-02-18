using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal class TypeInfo
        : ITypeInfo
    {
        public TypeInfo(
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
    }
}
