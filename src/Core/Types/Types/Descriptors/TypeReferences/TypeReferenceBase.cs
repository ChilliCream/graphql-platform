namespace HotChocolate.Types.Descriptors
{
    public class TypeReferenceBase
        : ITypeReference
    {
        protected TypeReferenceBase(
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            Context = context;
            IsTypeNullable = isTypeNullable;
            IsElementTypeNullable = isElementTypeNullable;
        }

        public TypeContext Context { get; }

        public bool? IsTypeNullable { get; }

        public bool? IsElementTypeNullable { get; }
    }
}
