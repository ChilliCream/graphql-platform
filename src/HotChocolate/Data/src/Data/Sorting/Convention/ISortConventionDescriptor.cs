using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// This descriptor is used to configure a <see cref="SortConvention"/>.
/// </summary>
public interface ISortConventionDescriptor
{
    /// <summary>
    /// Specifies an operation.
    /// </summary>
    /// <param name="operationId">
    /// The internal ID that is used to identify the operation.
    /// </param>
    ISortOperationConventionDescriptor Operation(int operationId);

    /// <summary>
    /// Binds a runtime type to all field of a <see cref="SortInputType"/> that do not have a
    /// more specific binding.
    /// </summary>
    /// <typeparam name="TSortType">The GraphQL sort type.</typeparam>
    ISortConventionDescriptor DefaultBinding<TSortType>();

    /// <summary>
    /// Binds a runtime type to a <see cref="SortInputType"/> so that the convention
    /// can infer the GraphQL type representation from internal runtime types
    /// like <see cref="string"/>.
    /// </summary>
    /// <typeparam name="TRuntimeType">The runtime type.</typeparam>
    /// <typeparam name="TSortType">The GraphQL sort type or the enum type.</typeparam>
    ISortConventionDescriptor BindRuntimeType<TRuntimeType, TSortType>();

    /// <summary>
    /// Binds a runtime type to a <see cref="SortInputType"/> so that the convention
    /// can infer the GraphQL type representation from internal runtime types
    /// like <see cref="string"/>.
    /// </summary>
    /// <param name="runtimeType">The runtime type.</param>
    /// <param name="sortType">GraphQL sort type or the enum type</param>
    ISortConventionDescriptor BindRuntimeType(Type runtimeType, Type sortType);

    /// <summary>
    /// Provides additional configuration for a sort enum type.
    /// </summary>
    /// <param name="configure">
    /// The configuration that shall be applied to the specified sort type.
    /// </param>
    /// <typeparam name="TSortEnumType">
    /// The sort type for which additional configuration shall be provided.
    /// </typeparam>
    ISortConventionDescriptor ConfigureEnum<TSortEnumType>(ConfigureSortEnumType configure)
        where TSortEnumType : SortEnumType;

    /// <summary>
    /// Provides additional configuration for a sort type.
    /// </summary>
    /// <param name="configure">
    /// The configuration that shall be applied to the specified sort type.
    /// </param>
    /// <typeparam name="TSortType">
    /// The sort type for which additional configuration shall be provided.
    /// </typeparam>
    ISortConventionDescriptor Configure<TSortType>(ConfigureSortInputType configure)
        where TSortType : SortInputType;

    /// <summary>
    /// Provides additional configuration for a sort type.
    /// </summary>
    /// <param name="configure">
    /// The configuration that shall be applied to the specified sort type.
    /// </param>
    /// <typeparam name="TSortType">
    /// The sort type for which additional configuration shall be provided.
    /// </typeparam>
    /// <typeparam name="TRuntimeType">
    /// The underlying runtime type of the sort type.
    /// </typeparam>
    ISortConventionDescriptor Configure<TSortType, TRuntimeType>(
        ConfigureSortInputType<TRuntimeType> configure)
        where TSortType : SortInputType<TRuntimeType>;

    /// <summary>
    /// Specifies the sort provider.
    /// </summary>
    /// <typeparam name="TProvider">The sort provider type.</typeparam>
    ISortConventionDescriptor Provider<TProvider>()
        where TProvider : class, ISortProvider;

    /// <summary>
    /// Specifies the sort provider.
    /// </summary>
    /// <param name="provider">The concrete sort provider that shall be used.</param>
    /// <typeparam name="TProvider">The sort provider type.</typeparam>
    ISortConventionDescriptor Provider<TProvider>(TProvider provider)
        where TProvider : class, ISortProvider;

    /// <summary>
    /// Specifies the sort provider.
    /// </summary>
    /// <param name="provider">The sort provider type.</param>
    ISortConventionDescriptor Provider(Type provider);

    /// <summary>
    /// Defines the argument name of the sort used by
    /// <see cref="SortingObjectFieldDescriptorExtensions.UseSorting(IObjectFieldDescriptor, string?)"/>
    /// </summary>
    /// <param name="argumentName">The argument name.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="argumentName"/> is <c>null</c> or
    /// <see cref="string.Empty"/>.
    /// </exception>
    ISortConventionDescriptor ArgumentName(string argumentName);

    /// <summary>
    /// Add a extensions that is applied to <see cref="SortProvider{TContext}"/>
    /// </summary>
    /// <typeparam name="TExtension">The sort provider extension type.</typeparam>
    ISortConventionDescriptor AddProviderExtension<TExtension>()
        where TExtension : class, ISortProviderExtension;

    /// <summary>
    /// Add a extensions that is applied to <see cref="SortProvider{TContext}"/>
    /// </summary>
    /// <param name="provider">
    /// The concrete sort provider extension that shall be used.
    /// </param>
    /// <typeparam name="TExtension">The sort provider extension type.</typeparam>
    ISortConventionDescriptor AddProviderExtension<TExtension>(TExtension provider)
        where TExtension : class, ISortProviderExtension;
}
