using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public interface ITypeDescriptor : ICodeDescriptor
    {
        /// <summary>
        /// Gets the type kind.
        /// </summary>
        public TypeKind Kind { get; }
    }
}
