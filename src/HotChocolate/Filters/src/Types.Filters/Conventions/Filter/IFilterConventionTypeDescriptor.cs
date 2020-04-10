using System;
using System.Collections.Generic;
using System.Text;

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
        /// Specifies a delegate of type <see cref="TryCreateImplicitFilter"/>. This delegate is
        /// invoked when ever filters are implicitly bound. <seealso cref="BindingBehavior"/>
        /// </summary>
        /// <param name="factory">The factory to create implicit filters</param>
        IFilterConventionTypeDescriptor TryCreateImplicitFilter(
            TryCreateImplicitFilter factory);

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
