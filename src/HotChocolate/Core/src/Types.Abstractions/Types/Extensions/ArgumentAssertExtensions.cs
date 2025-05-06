using System.Runtime.CompilerServices;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateTypeAbstractionsArgumentAssertExtensions
{
    public static IInputType ExpectInputType(
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

        return (IInputType)type;
    }

    public static IOutputType ExpectOutputType(
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

        return (IOutputType)type;
    }
}
