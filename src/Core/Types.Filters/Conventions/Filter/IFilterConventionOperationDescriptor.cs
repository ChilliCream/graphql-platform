using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionOperationDescriptor : IFluent
    {
        IFilterConventionOperationDescriptor Name(CreateFieldName factory);

        IFilterConventionOperationDescriptor Description(string value);

        /// <summary>
        /// Ignores the filter if true
        /// </summary> 
        /// 
        IFilterConventionOperationDescriptor Ignore(bool ignore = true);

        IFilterConventionDescriptor And();

        IFilterConventionOperationDescriptor AllowedFilter(AllowedFilterType value);

        IFilterConventionOperationDescriptor TryCreateImplicitFilter(
            TryCreateImplicitFilter factory);

    }
}
