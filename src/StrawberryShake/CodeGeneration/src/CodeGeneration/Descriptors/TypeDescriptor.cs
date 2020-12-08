namespace StrawberryShake.CodeGeneration
{
    public class TypeDescriptor
        : ICodeDescriptor
    {
        public string Name { get; }
        public bool IsNullable { get; }
        public ListType ListType { get; }
        public bool IsReferenceType { get; }

        public TypeDescriptor(string name, bool isNullable, ListType listType, bool isReferenceType)
        {
            Name = name;
            IsNullable = isNullable;
            ListType = listType;
            IsReferenceType = isReferenceType;
        }
    }
}
