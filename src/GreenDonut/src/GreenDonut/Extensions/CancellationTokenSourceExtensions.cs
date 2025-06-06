// ReSharper disable once CheckNamespace
namespace GreenDonut;

internal static class CancellationTokenSourceExtensions
{
    public static CancellationToken CreateLinkedCancellationToken(
        this CancellationTokenSource source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (cancellationToken == CancellationToken.None)
        {
            return source.Token;
        }

        return CancellationTokenSource.CreateLinkedTokenSource(
                source.Token,
                cancellationToken)
            .Token;
    }
}
