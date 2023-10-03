using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers;

internal static class TypeMergeHelpers
{
    private const int _maxRetries = 10000;

    public static string CreateName<T>(
        ISchemaMergeContext context,
        params T[] types)
        where T : ITypeInfo =>
        CreateName(context, (IReadOnlyList<T>)types);


    public static string CreateName<T>(
        ISchemaMergeContext context,
        IReadOnlyList<T> types)
        where T : ITypeInfo
    {
        var name = types[0].Definition.Name.Value;

        if (context.ContainsType(name))
        {
            for (var i = 0; i < types.Count; i++)
            {
                name = types[i].CreateUniqueName();
                if (!context.ContainsType(name))
                {
                    break;
                }
            }

            if (context.ContainsType(name))
            {
                name = types[0].Definition.Name.Value;

                for (var i = 0; i < _maxRetries; i++)
                {
                    var n = name + $"_{i}";
                    if (!context.ContainsType(name))
                    {
                        name = n;
                        break;
                    }
                }
            }
        }

        return name;
    }

    public static string CreateUniqueName(
        this ITypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return $"{typeInfo.Schema.Name}_{typeInfo.Definition.Name.Value}";
    }

    public static string CreateUniqueName(
        this ITypeInfo typeInfo,
        NamedSyntaxNode namedSyntaxNode)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        if (namedSyntaxNode == null)
        {
            throw new ArgumentNullException(nameof(namedSyntaxNode));
        }

        return $"{typeInfo.Schema.Name}_{namedSyntaxNode.Name.Value}";
    }
}
