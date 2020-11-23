using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public interface ISortOperationHandler
    {
        /// <summary>
        /// Tests if this operation handler can handle a field If it can handle the field it
        /// will be attached to the <see cref="SortField"/>
        /// </summary>
        /// <param name="context">The discovery context of the schema</param>
        /// <param name="typeDefinition">The definition of the declaring type of the field</param>
        /// <param name="valueDefinition">The definition of the field</param>
        /// <returns>Returns true if the field can be handled</returns>
        bool CanHandle(
            ITypeCompletionContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition);
    }
}
