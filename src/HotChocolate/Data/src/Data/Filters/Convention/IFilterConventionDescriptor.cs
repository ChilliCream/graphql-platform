using System;

namespace HotChocolate.Data.Filters
{
    public interface IFilterConventionDescriptor
    {
        IFilterOperationConventionDescriptor Operation(int operation);

        IFilterConventionDescriptor Binding<TRuntime, TInput>();

        /// <summary>
        /// Provides additional configuration for a filter type.
        /// </summary>
        /// <param name="configure">
        /// The configuration that shall be applied to the specified filter type.
        /// </param>
        /// <typeparam name="TFilterType">
        /// The filter type for which additional configuration shall be provided.
        /// </typeparam>
        IFilterConventionDescriptor Configure<TFilterType>(
            Action<IFilterInputTypeDescriptor> configure)
            where TFilterType : FilterInputType;

        /// <summary>
        /// Provides additional configuration for a filter type.
        /// </summary>
        /// <param name="typeName">
        /// The filter type for which additional configuration shall be provided.
        /// </param>
        /// <param name="configure">
        /// The configuration that shall be applied to the specified filter type.
        /// </param>
        IFilterConventionDescriptor Configure(
            NameString typeName,
            Action<IFilterInputTypeDescriptor> configure);

        /// <summary>
        /// Provides additional configuration for a filter type.
        /// </summary>
        /// <param name="configure">
        /// The configuration that shall be applied to the specified filter type.
        /// </param>
        /// <typeparam name="TFilterType">
        /// The filter type for which additional configuration shall be provided.
        /// </typeparam>
        /// <typeparam name="TRuntimeType">
        /// The underlying runtime type of the filter type.
        /// </typeparam>
        IFilterConventionDescriptor Configure<TFilterType, TRuntimeType>(
            Action<IFilterInputTypeDescriptor<TRuntimeType>> configure)
            where TFilterType : FilterInputType<TRuntimeType>;

        /// <summary>
        /// Specifies the filter provider.
        /// </summary>
        /// <typeparam name="TProvider">The filter provider type.</typeparam>
        IFilterConventionDescriptor Provider<TProvider>()
            where TProvider : class, IFilterProvider;

        /// <summary>
        /// Specifies the filter provider.
        /// </summary>
        /// <param name="provider">The concrete filter provider that shall be used.</param>
        /// <typeparam name="TProvider">The filter provider type.</typeparam>
        IFilterConventionDescriptor Provider<TProvider>(TProvider provider)
            where TProvider : class, IFilterProvider;

        /// <summary>
        /// Specifies the filter provider.
        /// </summary>
        /// <param name="provider">The filter provider type.</param>
        IFilterConventionDescriptor Provider(Type provider);

        /// <summary>
        /// Defines the argument name of the filter used by
        /// <see cref="FilterObjectFieldDescriptorExtensions.UseFiltering"/>
        /// </summary>
        /// <param name="argumentName">The argument name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="argumentName"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IFilterConventionDescriptor ArgumentName(NameString argumentName);

    }
}
