using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors.Helpers
{
    internal interface IFieldCompletion
    {
        void CompleteField(
            ITypeCompletionContext context,
            ITypeSystemMember declaringMember);
    }
}
