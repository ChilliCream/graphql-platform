namespace HotChocolate.Types;

public static class TypeNamePrinter
{
    private const int _maxTypeDepth = 6;

    public static string Print(this IType type) => Print(type, 0);

    private static string Print(IType type, int count)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (count > _maxTypeDepth)
        {
            throw new InvalidOperationException(
                "A type can only consist of four components.");
        }

        if (type is NonNullType nnt)
        {
            return $"{Print(nnt.Type, ++count)}!";
        }

        if (type is ListType lt)
        {
            return $"[{Print(lt.ElementType, ++count)}]";
        }

        if (type is INamedType n)
        {
            return n.Name;
        }

        throw new NotSupportedException(
            "The specified type is not supported.");
    }
}
