namespace HotChocolate.Stitching.Processing
{
    public readonly struct Field
    {
        public Field(NameString typeName, NameString fieldName)
        {
            TypeName = typeName;
            FieldName = fieldName;
        }

        public NameString TypeName { get; }
        public NameString FieldName { get; }
    }
}
