namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionTypeDescriptor : IFluent
    {
        /// <summary>
        /// Ignores the current filter kind
        /// </summary> 
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param>
        IFilterConventionTypeDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Ignores a operation kind on current filter kind
        /// </summary>
        /// <param name="kind">The operation kind to ignore</param>
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param>
        IFilterConventionTypeDescriptor Ignore(int kind, bool ignore = true);

        /// <summary>
        /// Specifies the configuration of a operation kind for current
        /// filter kind
        /// </summary> 
        /// <param name="kind">The operation kind to configure</param>
        IFilterConventionOperationDescriptor Operation(int kind);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterConventionDescriptor"/>
        /// </summary>
        IFilterConventionDescriptor And();
    }
}
