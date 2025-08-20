using HotChocolate.Configuration;

namespace HotChocolate.Types.Helpers;

internal interface IEnumValueCompletion
{
    void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);
}
