using System.Collections.Generic;

namespace HotChocolate.Types.Selections
{
    internal static class ListHandlers
    {
        public static IReadOnlyList<IListHandler> All { get; } =
            new IListHandler[]
            {
                new FilterHandler(),
                new SortHandler(),
                new SingleOrDefaultHandler()
            };
    }
}
