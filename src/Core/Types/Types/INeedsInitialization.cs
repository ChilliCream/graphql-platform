namespace HotChocolate.Types
{
    /// <summary>
    /// Types that implement this have a three step initialization.
    /// 1. Creation of the type instance.
    /// 2. Registration of types that are needed by this type.
    /// 3. The type will be completed by resolving all
    ///    referenced types, creation of all field resolvers that
    ///    the type may need and by making the type instance immutable.
    /// </summary>
    internal interface INeedsInitialization
    {
        /// <summary>
        /// Registers types that are used by the
        /// implementing type with the schema context.
        /// This will be called before the type is completed.
        /// </summary>
        void RegisterDependencies(ITypeInitializationContext context);

        /// <summary>
        /// Completes the type and this makes it immutable.
        /// The type ca
        /// </summary>
        void CompleteType(ITypeInitializationContext context);
    }
}
