using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionTypeDescriptor : IFluent
    {
        IFilterConventionTypeDescriptor Name(NameString factory);

        IFilterConventionTypeDescriptor Description(string value);

        /// <summary>
        /// Ignores the filter type if true
        /// </summary> 
        /// 
        IFilterConventionTypeDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Ignores the filter operation if true
        /// </summary> 
        /// 
        IFilterConventionTypeDescriptor Ignore(FilterOperationKind kind, bool ignore = true);

        IFilterConventionDescriptor And();

        IFilterConventionTypeDescriptor TryCreateImplicitFilter(
            TryCreateImplicitFilter factory);

        IFilterConventionOperationDescriptor Operation(FilterOperationKind kind);


    }
}
