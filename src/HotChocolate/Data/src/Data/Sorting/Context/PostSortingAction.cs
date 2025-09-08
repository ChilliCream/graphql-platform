namespace HotChocolate.Data.Sorting;

public delegate TQuery PostSortingAction<TQuery>(bool userDefinedSorting, TQuery query);
