using System.Collections.Generic;

namespace HotChocolate.Types.Selections.Handlers
{
    internal static class ListHandlers
    {
        public static IReadOnlyList<IListHandler> All { get; } =
            new IListHandler[]
            {
                new FilterHandler(),
                new SortHandler(),
                new SingleOrDefaultHandler(),
                new FirstOrDefaultHandler()
            };
    }
}
