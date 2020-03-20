namespace StrawberryShake.CodeGeneration
{
    public class ResultTypeDescriptor
        : ICodeDescriptor
    {
        public ResultTypeDescriptor(
            string name,
            bool isNullable,
            bool isList,
            bool isReferenceType)
        {
            Name = name;
            IsNullable = isNullable;
            IsList = isList;
            IsReferenceType = isReferenceType;
        }

        public string Name { get; }

        public bool IsNullable { get; }

        public bool IsList { get; }

        public bool IsReferenceType { get; }
    }
}
