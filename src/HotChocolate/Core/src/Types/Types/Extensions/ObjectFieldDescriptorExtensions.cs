using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Provides configuration methods to <see cref="IObjectFieldDescriptor"/>.
/// </summary>
public static class ObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Marks a field as serial executable which will ensure that the execution engine
    /// synchronizes resolver execution around the marked field and ensures that
    /// no other field is executed in parallel.
    /// </summary>
    public static IObjectFieldDescriptor Serial(this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().OnBeforeCreate(c => c.IsParallelExecutable = false);
        return descriptor;
    }

    /// <summary>
    /// Marks a field as parallel executable which will allow the execution engine
    /// to execute this field in parallel with other resolvers.
    /// </summary>
    public static IObjectFieldDescriptor Parallel(this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().OnBeforeCreate(c => c.IsParallelExecutable = true);
        return descriptor;
    }

    /// <summary>
    /// Specifies the type of an object field with GraphQL SDL type syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="typeSyntax">
    /// The GraphQL SDL type syntax.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL SDL type syntax is invalid.
    /// </exception>
    public static IObjectFieldDescriptor Type(
        this IObjectFieldDescriptor descriptor,
        string typeSyntax)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (typeSyntax is null)
        {
            throw new ArgumentNullException(nameof(typeSyntax));
        }

        return descriptor.Type(Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax));
    }

    /// <summary>
    /// Specifies the type of an argument with GraphQL SDL type syntax.
    /// </summary>
    /// <param name="descriptor">
    /// The argument descriptor.
    /// </param>
    /// <param name="typeSyntax">
    /// The GraphQL SDL type syntax.
    /// </param>
    /// <returns>
    /// Returns the argument descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// <paramref name="typeSyntax"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="SyntaxException">
    /// The GraphQL SDL type syntax is invalid.
    /// </exception>
    public static IArgumentDescriptor Type(
        this IArgumentDescriptor descriptor,
        string typeSyntax)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (typeSyntax is null)
        {
            throw new ArgumentNullException(nameof(typeSyntax));
        }

        return descriptor.Type(Utf8GraphQLParser.Syntax.ParseTypeReference(typeSyntax));
    }

    /// <summary>
    /// Specifies that the resolver of this field shall use services from the request service scope.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor UseRequestScope(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().Definition.DependencyInjectionScope = DependencyInjectionScope.Request;
        return descriptor;
    }

    /// <summary>
    /// Specifies that the resolver of this field shall use services from the resolver service scope.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor UseResolverScope(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().Definition.DependencyInjectionScope = DependencyInjectionScope.Resolver;
        return descriptor;
    }
}
