using System.Runtime.CompilerServices;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateTypeAbstractionsArgumentAssertExtensions
{
    public static IInputType ExpectInputType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
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
        [CallerArgumentExpression(nameof(type))] string name = "type")
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

    public static IComplexTypeDefinition ExpectComplexType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsComplexType())
        {
            throw new ArgumentException("Must be a complex type.", name);
        }

        return (IComplexTypeDefinition)type;
    }

    public static IInputObjectTypeDefinition ExpectInputObjectType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsInputObjectType())
        {
            throw new ArgumentException("Must be an input object type.", name);
        }

        return (IInputObjectTypeDefinition)type;
    }

    public static IObjectTypeDefinition ExpectObjectType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsObjectType())
        {
            throw new ArgumentException("Must be an object type.", name);
        }

        return (IObjectTypeDefinition)type;
    }

    public static IInterfaceTypeDefinition ExpectInterfaceType(
        this IType type,
        [CallerArgumentExpression("type")] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsInterfaceType())
        {
            throw new ArgumentException("Must be an interface type.", name);
        }

        return (IInterfaceTypeDefinition)type;
    }

    public static IUnionTypeDefinition ExpectUnionType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsUnionType())
        {
            throw new ArgumentException("Must be a union type.", name);
        }

        return (IUnionTypeDefinition)type;
    }

    public static IEnumTypeDefinition ExpectEnumType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsEnumType())
        {
            throw new ArgumentException("Must be an enum type.", name);
        }

        return (IEnumTypeDefinition)type;
    }

    public static IScalarTypeDefinition ExpectScalarType(
        this IType type,
        [CallerArgumentExpression(nameof(type))] string name = "type")
    {
        if (type is null)
        {
            throw new ArgumentNullException(name);
        }

        if (!type.IsScalarType())
        {
            throw new ArgumentException("Must be a scalar type.", name);
        }

        return (IScalarTypeDefinition)type;
    }
}
