using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
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
}
