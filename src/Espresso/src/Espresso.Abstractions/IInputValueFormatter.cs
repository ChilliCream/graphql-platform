using System.Collections.Immutable;

namespace Espresso.Abstractions;

public interface IInputValueFormatter<in TIn, out TOut>
{
    TOut Format(TIn runtimeValue);
}

public class GetProductQuery
{
    public ValueTask<OperationResult> QueryAsync(
        FooVariables variables,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public record OperationResult
{
    public ImmutableArray<object> Errors { get; init; } = ImmutableArray<object>.Empty;

    public ImmutableDictionary<string, object?> Extensions { get; init; } = ImmutableDictionary<string, object?>.Empty;
}

public record FooVariables(string Id);

public class FooOperationResult
{
    public Product? ProductById { get; set; }
}

public class Product
{
    public string Id { get; set; }
}
