using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public static class OrderBuilder
    {
        private static readonly List<SortItem> _sortItems = new();
        private static SortItem _lastSortItem;
        private static Skip _skip;
        private static Limit _limit;

        static void Reset()
        {
            _sortItems.Clear();
            _lastSortItem = null;
            _skip = null;
            _limit = null;
        }
    }
}
