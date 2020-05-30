using System;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterConventionOperationDescriptorBase : IFluent
    {
        /// <summary>
        /// Specifies a delegate that return the name of a field for a specific 
        /// operation kind
        /// </summary> 
        /// <param name="factory">The delegate to name the field</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="factory"/> is <c>null</c>
        /// </exception>
        IFilterConventionOperationDescriptorBase Name(CreateFieldName factory);

        /// <summary>
        /// Specify the graphql description of a operation kind
        /// </summary> 
        /// <param name="description">The description to name the field</param> 
        IFilterConventionOperationDescriptorBase Description(string description);
    }
}
