using HotChocolate.Configuration;

#nullable enable

namespace HotChocolate.Types.Helpers;

internal interface IEnumValueCompletion
{
    void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);
}
