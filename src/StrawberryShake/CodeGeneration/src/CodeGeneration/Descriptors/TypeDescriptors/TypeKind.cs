namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    /// <summary>
    /// Represents the generated type kind.
    /// </summary>
    public enum TypeKind
    {
        /// <summary>
        /// A leaf type is a scalar or enum.
        /// </summary>
        Leaf,

        /// <summary>
        /// A data interface or union type.
        /// </summary>
        AbstractData,

        /// <summary>
        /// A concrete data class.
        /// </summary>
        Data,

        /// <summary>
        /// An entity interface or class.
        /// </summary>
        Entity,

        /// <summary>
        /// An abstract type that can be a entity or a data type.
        /// </summary>
        EntityOrData,

        /// <summary>
        /// A result interface or class.
        /// </summary>
        Result,

        /// <summary>
        /// A input class.
        /// </summary>
        Input
    }
}
