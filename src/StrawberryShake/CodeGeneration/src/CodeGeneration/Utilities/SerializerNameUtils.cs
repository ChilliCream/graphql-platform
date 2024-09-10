using System.Text;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class SerializerNameUtils
{
    public static string CreateDeserializerName(IType type) =>
        CreateName(type, "Deserialize");

    public static string CreateSerializerName(IType type) =>
        CreateName(type, "Serialize");

    private static string CreateName(IType type, string prefix)
    {
        var current = type;
        var types = new Stack<IType>();

        var sb = new StringBuilder();
        sb.Append(prefix);

        while (!(current is INamedType))
        {
            if (current is HotChocolate.Types.ListType)
            {
                if (types.Count == 0 || !(types.Peek() is NonNullType))
                {
                    sb.Append("Nullable");
                }
                sb.Append("ListOf");
            }
            types.Push(current);
            current = current.InnerType();
        }

        if (types.Count == 0 || !(types.Peek() is NonNullType))
        {
            sb.Append("Nullable");
        }
        sb.Append(type.NamedType().Name);

        return sb.ToString();
    }
}
