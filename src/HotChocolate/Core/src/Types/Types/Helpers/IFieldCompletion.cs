using HotChocolate.Configuration;

namespace HotChocolate.Types.Helpers;

internal interface IFieldCompletion
{
    void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);
}
