namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterExpressionTypeDescriptor : IFluent
    {
        /// <summary>
        /// Specifies the configuration of a <see cref="FilterOperationKind"/> for current
        /// <see cref="FilterKind"/>
        /// </summary> 
        /// <param name="kind">The <see cref="FilterOperationKind"/> to configure</param>
        IFilterExpressionOperationDescriptor Operation(FilterOperationKind kind);

        /// <summary>
        /// Specifies the enter behavior of the current field. The default action is <c>SkipAndLeave</c>
        /// </summary>
        /// <param name="handler">A delegate of type <see cref="FilterFieldEnter"/></param>
        IFilterExpressionTypeDescriptor Enter(FilterFieldEnter handler);

        /// <summary>
        /// Specifies the leave method of the current field.
        /// </summary>
        /// <param name="handler">A delegate of type <see cref="FilterFieldLeave"/></param>
        IFilterExpressionTypeDescriptor Leave(FilterFieldLeave handler);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterVisitorDescriptor"/>
        /// </summary>
        IFilterExpressionVisitorDescriptor And();
    }
}
