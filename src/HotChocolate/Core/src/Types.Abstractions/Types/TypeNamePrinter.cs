namespace HotChocolate.Types;

public static class TypeNamePrinter
{
    private const int MaxTypeDepth = 6;

    public static string Print(this IType type) => Print(type, 0);

    private static string Print(IType type, int count)
    {
        if (count > MaxTypeDepth)
        {
            throw new InvalidOperationException(
                "A type can only consist of four components.");
        }

        if (type is NonNullType nnt)
        {
            return $"{Print(nnt.NullableType, ++count)}!";
        }

        if (type is ListType lt)
        {
            return $"[{Print(lt.ElementType, ++count)}]";
        }

        if (type is ITypeDefinition n)
        {
            return n.Name;
        }

        throw new NotSupportedException(
            "The specified type is not supported.");
    }
}
