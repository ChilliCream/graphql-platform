namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionTypeDescriptor : IFluent
    {
        /// <summary>
        /// Ignores the current <see cref="FilterKind"/>
        /// </summary> 
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param>
        IFilterConventionTypeDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Ignores a <see cref="FilterOperationKind"/> on current <see cref="FilterKind"/>
        /// </summary>
        /// <param name="kind">The <see cref="FilterOperationKind"/> to ignore</param>
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param>
        IFilterConventionTypeDescriptor Ignore(FilterOperationKind kind, bool ignore = true);

        /// <summary>
        /// Specifies the configuration of a <see cref="FilterOperationKind"/> for current
        /// <see cref="FilterKind"/>
        /// </summary> 
        /// <param name="kind">The <see cref="FilterOperationKind"/> to configure</param>
        IFilterConventionOperationDescriptor Operation(FilterOperationKind kind);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterConventionDescriptor"/>
        /// </summary>
        IFilterConventionDescriptor And();
    }
}
