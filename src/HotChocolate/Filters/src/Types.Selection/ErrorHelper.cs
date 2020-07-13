using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selections
{
    internal static class ErrorHelper
    {
        public static IError CreateMoreThanOneError(IResolverContext context) =>
            ErrorBuilder.New()
                .SetMessage("Sequence contains more than one element.")
                .SetCode("SELECTIONS_SINGLE_MORE_THAN_ONE")
                .SetPath(context.Path)
                .AddLocation(context.FieldSelection)
                .Build();
    }
}
