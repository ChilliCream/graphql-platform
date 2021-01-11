using System;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// This descriptor is used to configure a <see cref="FilterConvention"/>.
    /// </summary>
    public interface IFilterConventionDescriptor
    {
        /// <summary>
        /// Specifies an operation.
        /// </summary>
        /// <param name="operationId">
        /// The internal ID that is used to identify the operation.
        /// </param>
        IFilterOperationConventionDescriptor Operation(int operationId);

        /// <summary>
        /// Binds a runtime type to a <see cref="FilterInputType"/> so that the convention
        /// can infer the GraphQL type representation from internal runtime types
        /// like <see cref="System.String"/>.
        /// </summary>
        /// <typeparam name="TRuntimeType">The runtime type.</typeparam>
        /// <typeparam name="TFilterType">The GraphQL filter type.</typeparam>
        IFilterConventionDescriptor BindRuntimeType<TRuntimeType, TFilterType>()
            where TFilterType : FilterInputType;

        /// <summary>
        /// Binds a runtime type to a <see cref="FilterInputType"/> so that the convention
        /// can infer the GraphQL type representation from internal runtime types
        /// like <see cref="System.String"/>.
        /// </summary>
        /// <param name="runtimeType">The runtime type.</param>
        /// <param name="filterType">GraphQL filter type.</param>
        IFilterConventionDescriptor BindRuntimeType(Type runtimeType, Type filterType);

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
            ConfigureFilterInputType configure)
            where TFilterType : FilterInputType;

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
            ConfigureFilterInputType<TRuntimeType> configure)
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

        /// <summary>
        /// Add a extensions that is applied to <see cref="FilterProvider{TContext}"/>
        /// </summary>
        /// <typeparam name="TExtension">The filter provider extension type.</typeparam>
        IFilterConventionDescriptor AddProviderExtension<TExtension>()
            where TExtension : class, IFilterProviderExtension;

        /// <summary>
        /// Add a extensions that is applied to <see cref="FilterProvider{TContext}"/>
        /// </summary>
        /// <param name="provider">
        /// The concrete filter provider extension that shall be used.
        /// </param>
        /// <typeparam name="TExtension">The filter provider extension type.</typeparam>
        IFilterConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
            where TExtension : class, IFilterProviderExtension;

        /// <summary>
        /// Defines if OR-combinators are allowed for filtering.
        /// </summary>
        /// <param name="allow">
        /// Specifies if OR-combinators are allowed or disallowed.
        /// </param>
        IFilterConventionDescriptor AllowOr(bool allow = true);

        /// <summary>
        /// Defines if AND-combinators are allowed for filtering.
        /// </summary>
        /// <param name="allow">
        /// Specifies if AND-combinators are allowed or disallowed.
        /// </param>
        IFilterConventionDescriptor AllowAnd(bool allow = true);

    }
}
