using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionOperationDescriptor
        : IFilterConventionOperationDescriptorBase
    {
        /// <inheritdoc/>
        new IFilterConventionOperationDescriptor Name(CreateFieldName factory);

        /// <inheritdoc/>
        new IFilterConventionOperationDescriptor Description(string value);

        /// <summary>
        /// Ignores a <see cref="FilterOperationKind"/>
        /// </summary>
        /// <param name="ignore"><c>true</c> to ignore or <c>false</c> to unignore</param>
        IFilterConventionOperationDescriptor Ignore(bool ignore = true);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterConventionTypeDescriptor"/>
        /// </summary>
        IFilterConventionTypeDescriptor And();
    }
}
