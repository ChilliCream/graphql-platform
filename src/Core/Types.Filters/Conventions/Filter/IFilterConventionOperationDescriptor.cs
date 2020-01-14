using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionOperationDescriptor
        : IFilterConventionOperationDescriptorBase
    {
        new IFilterConventionOperationDescriptor Name(CreateFieldName factory);

        new IFilterConventionOperationDescriptor Description(string value);

        /// <summary>
        /// Ignores the filter if true
        /// </summary> 
        /// 
        IFilterConventionOperationDescriptor Ignore(bool ignore = true);

        IFilterConventionTypeDescriptor And();

    }
}
