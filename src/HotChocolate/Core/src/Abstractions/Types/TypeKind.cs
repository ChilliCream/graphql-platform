namespace HotChocolate.Types
{
    public enum TypeKind
    {
        /// <summary>
        /// Indicates this type is an interface. `fields` and `possibleTypes` are valid fields.
        /// </summary>
        Interface = 0,


        Object = 1,

        /// <summary>
        /// Indicates this type is a union. `possibleTypes` is a valid field.
        /// </summary>
        Union = 2,

        /// <summary>
        /// Indicates this type is an input object. `inputFields` is a valid field.
        /// </summary>
        InputObject = 4,

        /// <summary>
        /// Indicates this type is an enum. `enumValues` is a valid field.
        /// </summary>
        Enum = 8,

        Scalar = 16,

        /// <summary>
        /// Indicates this type is a list. `ofType` is a valid field.
        /// </summary>
        List = 32,

        /// <summary>
        /// Indicates this type is a non-null. `ofType` is a valid field.
        /// </summary>
        NonNull = 64,
        
        Directive = 128
    }
}
