using System;
using System.Runtime.Serialization;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    [Serializable]
    public class SchemaMergeException
        : Exception
    {
        public SchemaMergeException(
            ITypeDefinitionNode typeDefinition,
            ITypeExtensionNode typeExtension,
            string message)
            : base(message)
        {
            TypeDefinition = typeDefinition
                ?? throw new ArgumentNullException(nameof(typeDefinition));
            TypeExtension = typeExtension
                ?? throw new ArgumentNullException(nameof(typeExtension));
        }


        protected SchemaMergeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }

        public ITypeDefinitionNode TypeDefinition { get; }

        public ITypeExtensionNode TypeExtension { get; }
    }
}
