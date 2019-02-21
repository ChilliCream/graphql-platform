namespace HotChocolate.Types
{
    public class FieldDescriptionBase
        : TypeDescriptionBase
    {
        protected FieldDescriptionBase() { }

        public TypeReference Type { get; set; }

        public bool Ignored { get; set; }

        public bool? IsTypeNullable { get; set; }

        public bool? IsElementTypeNullable { get; set; }
    }
}
