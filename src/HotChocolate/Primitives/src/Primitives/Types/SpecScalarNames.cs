namespace HotChocolate.Types;

/// <summary>
/// Provides well-known spec scalar names.
/// </summary>
public static class SpecScalarNames
{
    /// <summary>
    /// The name constants of the String scalar.
    /// </summary>
    public static class String
    {
        /// <summary>
        /// The name of the String scalar.
        /// </summary>
        public const string Name = "String";
    }

    /// <summary>
    /// The name constants of the Int scalar.
    /// </summary>
    public static class Int
    {
        /// <summary>
        /// The name of the Int scalar.
        /// </summary>
        public const string Name = "Int";
    }

    /// <summary>
    /// The name constants of the Float scalar.
    /// </summary>
    public static class Float
    {
        /// <summary>
        /// The name of the Float scalar.
        /// </summary>
        public const string Name = "Float";
    }

    /// <summary>
    /// The name constants of the Boolean scalar.
    /// </summary>
    public static class Boolean
    {
        /// <summary>
        /// The name of the Boolean scalar.
        /// </summary>
        public const string Name = "Boolean";
    }

    /// <summary>
    /// The name constants of the ID scalar.
    /// </summary>
    public static class ID
    {
        /// <summary>
        /// The name of the ID scalar.
        /// </summary>
        public const string Name = "ID";
    }

    public static bool IsSpecScalar(string name)
        => name switch
        {
            String.Name => true,
            Boolean.Name => true,
            Float.Name => true,
            ID.Name => true,
            Int.Name => true,
            _ => false
        };
}
