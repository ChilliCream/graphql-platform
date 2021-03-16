namespace HotChocolate.Configuration
{
    /// <summary>
    /// Defines the directive visibility.
    /// </summary>
    public enum DirectiveVisibility
    {
        /// <summary>
        /// Directive is public and visible within the type system and through introspection.
        /// </summary>
        Public,

        /// <summary>
        /// Directive is internal and only visible within the type system.
        /// </summary>
        Internal
    }
}
