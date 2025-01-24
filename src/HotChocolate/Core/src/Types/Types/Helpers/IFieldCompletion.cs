using HotChocolate.Configuration;

namespace HotChocolate.Types.Helpers;

/// <summary>
/// This interface is explicitly implemented by fields
/// and is used to trigger the initialization hooks.
/// </summary>
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
