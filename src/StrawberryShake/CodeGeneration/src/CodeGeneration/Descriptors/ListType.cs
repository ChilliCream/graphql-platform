namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes the type of list a property can be
    /// </summary>
    public enum ListType
    {
        /// <summary>
        /// The property is no list.
        /// </summary>
        NoList,

        /// <summary>
        /// The property is a non nullable list
        /// </summary>
        List,

        /// <summary>
        /// The property is a nullable list
        /// </summary>
        NullableList
    }
}
