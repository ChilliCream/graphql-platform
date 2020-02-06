namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionDescriptor : IFluent
    {
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
        /// Defines the base name of the element in array filters.
        /// </summary> 
        /// <param name="name">The object type name.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <c>null</c> or
        /// <see cref="string.Empty"/>.
        /// </exception>
        IFilterConventionDescriptor ElementName(
                NameString name);

        /// <summary>
        /// Specifies a delegate that returns the graphql type name of a
        /// <see cref="FilterInputType{T}"/>
        /// </summary> 
        /// <param name="factory">The delegate to name the type</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        IFilterConventionDescriptor FilterTypeName(
            GetFilterTypeName factory);

        /// <summary>
        /// Specifies a delegate that returns the graphql description of a
        /// <see cref="FilterInputType{T}"/>
        /// </summary> 
        /// <param name="factory">The delegate to name the type</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        IFilterConventionDescriptor FilterTypeDescription(
            GetFilterTypeDescription factory);

        /// <summary>
        /// Specifies the configuration of a <see cref="FilterKind"/> with a
        /// <see cref="FilterConventionTypeDescriptor"/>
        /// </summary> 
        /// <param name="kind">The <see cref="FilterKind"/> to configure</param>  
        IFilterConventionTypeDescriptor Type(FilterKind kind);

        /// <summary>
        /// Removes all configuration (including default configuration!)
        /// Apply <see cref="FilterConventionExtensions.UseDefault"/> to add the defaults.
        /// </summary>
        IFilterConventionDescriptor Reset();

        /// <summary>
        /// Ignores a <see cref="FilterKind"/>  
        /// </summary>
        /// <param name="kind">The <see cref="FilterKind"/> to ignore</param>
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param> 
        IFilterConventionDescriptor Ignore(FilterKind kind, bool ignore = true);

        /// <summary>
        /// Configures the default behaviour of an <see cref="FilterOperationKind"/> 
        /// </summary>
        /// <param name="kind">The <see cref="FilterOperationKind"/> to configure</param> 
        IFilterConventionDefaultOperationDescriptor Operation(FilterOperationKind kind);
    }
}
