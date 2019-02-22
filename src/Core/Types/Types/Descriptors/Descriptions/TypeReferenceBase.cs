namespace HotChocolate.Types.Descriptors
{
    public class TypeReferenceBase
        : ITypeReference
    {
        protected TypeReferenceBase(
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            IsTypeNullable = isTypeNullable;
            IsElementTypeNullable = isElementTypeNullable;
        }

        public bool? IsTypeNullable { get; }

        public bool? IsElementTypeNullable { get; }
    }

}
