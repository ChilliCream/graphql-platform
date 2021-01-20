namespace HotChocolate.Configuration
{
    /// <summary>
    /// The type initialization flow interceptor is triggered for each type initialization step.
    /// This interceptor can be useful to aggregate or process data in combination with the
    /// <see cref="ITypeInitializationInterceptor"/>.
    /// </summary>
    public interface ITypeInitializationFlowInterceptor
    {
        /// <summary>
        /// This method is called before the type discovery is started.
        /// </summary>
        void OnBeforeDiscoverTypes();

        /// <summary>
        /// This method is called after the type discovery is finished.
        /// </summary>
        void OnAfterDiscoverTypes();

        /// <summary>
        /// This method is called before the type names are completed.
        /// </summary>
        void OnBeforeCompleteTypeNames();

        /// <summary>
        /// This method is called after the type names are completed.
        /// </summary>
        void OnAfterCompleteTypeNames();

        /// <summary>
        /// This method is called before the type extensions are merged.
        /// </summary>
        void OnBeforeMergeTypeExtensions();

        /// <summary>
        /// This method is called after the type extensions are merged.
        /// </summary>
        void OnAfterMergeTypeExtensions();

        /// <summary>
        /// This method is called before the types are completed.
        /// </summary>
        void OnBeforeCompleteTypes();

        /// <summary>
        /// This method is called after the types are completed.
        /// </summary>
        void OnAfterCompleteTypes();
    }
}
