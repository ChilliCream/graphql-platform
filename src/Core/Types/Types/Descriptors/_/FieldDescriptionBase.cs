using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class FieldDescriptionBase
        : TypeDescriptionBase
    {
        protected FieldDescriptionBase() { }

        public ITypeReference Type { get; set; }

        public bool Ignore { get; set; }
    }
}
