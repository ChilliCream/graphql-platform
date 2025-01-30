namespace GreenDonut.Data.Internal;

internal sealed record PagingState<T>(PagingArguments PagingArgs, QueryContext<T>? Context = null);
