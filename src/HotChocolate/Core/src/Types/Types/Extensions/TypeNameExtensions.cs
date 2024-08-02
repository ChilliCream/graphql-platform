using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public static class TypeNameExtensions
{
    public static IObjectTypeNameDependencyDescriptor Name(
        this IObjectTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new ObjectTypeNameDependencyDescriptor(
            descriptor, createName);
    }

    public static IObjectTypeNameDependencyDescriptor<T> Name<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new ObjectTypeNameDependencyDescriptor<T>(
            descriptor, createName);
    }

    public static IEnumTypeNameDependencyDescriptor Name(
        this IEnumTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new EnumTypeNameDependencyDescriptor(
            descriptor, createName);
    }

    public static IEnumTypeNameDependencyDescriptor<T> Name<T>(
        this IEnumTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new EnumTypeNameDependencyDescriptor<T>(
            descriptor, createName);
    }

    public static IInputObjectTypeNameDependencyDescriptor Name(
        this IInputObjectTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new InputObjectTypeNameDependencyDescriptor(
            descriptor, createName);
    }

    public static IInputObjectTypeNameDependencyDescriptor<T> Name<T>(
        this IInputObjectTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new InputObjectTypeNameDependencyDescriptor<T>(
            descriptor, createName);
    }

    public static IInterfaceTypeNameDependencyDescriptor Name(
        this IInterfaceTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new InterfaceTypeNameDependencyDescriptor(
            descriptor, createName);
    }

    public static IInterfaceTypeNameDependencyDescriptor<T> Name<T>(
        this IInterfaceTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new InterfaceTypeNameDependencyDescriptor<T>(
            descriptor, createName);
    }

    public static IUnionTypeNameDependencyDescriptor Name(
        this IUnionTypeDescriptor descriptor,
        Func<INamedType, string> createName)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (createName is null)
        {
            throw new ArgumentNullException(nameof(createName));
        }

        return new UnionTypeNameDependencyDescriptor(
            descriptor, createName);
    }
}
