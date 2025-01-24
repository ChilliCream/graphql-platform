using HotChocolate.Configuration;

namespace HotChocolate.Types.Helpers;

internal interface IFieldCompletion
{
    void CompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);

    void CompleteMetadata(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);

    void MakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);

    void Finalize(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember);
}
