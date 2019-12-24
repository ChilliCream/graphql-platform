using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionOperationDescriptorBase : IFluent
    {
        IFilterConventionOperationDescriptorBase Name(CreateFieldName factory);

        IFilterConventionOperationDescriptorBase Description(string value);

        /// <summary>
        /// Ignores the filter if true
        /// </summary> 
        /// 
        IFilterConventionOperationDescriptorBase Ignore(bool ignore = true);

    }
}
