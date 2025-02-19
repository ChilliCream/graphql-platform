using System.Runtime.CompilerServices;

namespace HotChocolate.Types.Mutable;

internal static class ArgumentAssertExtensions
{
    public static IType ExpectInputType(
        this IType type,
        [CallerArgumentExpression("type")] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsInputType())
        {
            throw new ArgumentException("Must be an input type.", name);
        }

        return type;
    }

    public static IType ExpectOutputType(
        this IType type,
        [CallerArgumentExpression("type")] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsOutputType())
        {
            throw new ArgumentException("Must be an output type.", name);
        }

        return type;
    }
}
