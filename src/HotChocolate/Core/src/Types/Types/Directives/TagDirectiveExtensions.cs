using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods to add a @tag(name: "your-value") directive to a type system member.
/// </summary>
public static class TagDirectiveExtensions
{
    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="ObjectType"/>.
    /// <code>
    /// type Book @tag(name: "your-value") {
    ///   id: ID!
    ///   title: String!
    ///   author: String!
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be applied.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectTypeDescriptor Tag(
        this IObjectTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="InterfaceType"/>.
    /// <code>
    /// interface Product @tag(name: "your-value") {
    ///   id: ID!
    ///   name: String
    ///   dimension: ProductDimension
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be applied.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IInterfaceTypeDescriptor Tag(
        this IInterfaceTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="UnionType"/>.
    /// <code>
    /// union ReadingMaterial = Book | Magazine @tag(name: "your-value")
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IUnionTypeDescriptor Tag(
        this IUnionTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="InputObjectType"/>.
    /// <code>
    /// input BookInput @tag(name: "your-value") {
    ///   title: String!
    ///   author: String!
    ///   publishedDate: String
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IInputObjectTypeDescriptor Tag(
        this IInputObjectTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="EnumType"/>.
    /// <code>
    /// enum Episode @tag(name: "your-value") {
    ///   NEWHOPE
    ///   EMPIRE
    ///   JEDI
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEnumTypeDescriptor Tag(
        this IEnumTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="ObjectField"/>.
    /// <code>
    /// type Book {
    ///   id: ID! @tag(name: "your-value")
    ///   title: String!
    ///   author: String!
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Tag(
        this IObjectFieldDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="InterfaceType"/>.
    /// <code>
    /// interface Book {
    ///   id: ID! @tag(name: "your-value")
    ///   title: String!
    ///   author: String!
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IInterfaceFieldDescriptor Tag(
        this IInterfaceFieldDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="InputField"/>.
    /// <code>
    /// input BookInput {
    ///   title: String! @tag(name: "your-value")
    ///   author: String!
    ///   publishedDate: String
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IInputFieldDescriptor Tag(
        this IInputFieldDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="Argument"/>.
    /// <code>
    /// type Query {
    ///   books(search: String! @tag(name: "your-value")): [Book]
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IArgumentDescriptor Tag(
        this IArgumentDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="DirectiveArgument"/>.
    /// <code>
    /// directive @description(value: String! @tag(name: "your-value")) on FIELD_DEFINITION
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IDirectiveArgumentDescriptor Tag(
        this IDirectiveArgumentDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="EnumValue"/>.
    /// <code>
    /// enum Episode {
    ///   NEWHOPE @tag(name: "your-value")
    ///   EMPIRE
    ///   JEDI
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEnumValueDescriptor Tag(
        this IEnumValueDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    /// <summary>
    /// Adds a @tag(name: "your-value") to an <see cref="EnumValue"/>.
    /// <code>
    /// schema @myDirective(arg: "value") {
    ///   query: Query
    ///   mutation: Mutation
    ///   subscription: Subscription
    /// }
    /// enum Episode {
    ///   NEWHOPE @tag(name: "your-value")
    ///   EMPIRE
    ///   JEDI
    /// }
    /// </code>
    /// </summary>
    /// <param name="descriptor">
    /// The <paramref name="descriptor"/> on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// The value represents the <paramref name="name"/> of the tag.
    /// </param>
    /// <returns>
    /// Returns the <paramref name="descriptor"/> on which this directive
    /// was applied for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static ISchemaTypeDescriptor Tag(
        this ISchemaTypeDescriptor descriptor,
        string name)
    {
        ApplyTag(descriptor, name);
        return descriptor;
    }

    private static void ApplyTag(
        this IDescriptor descriptor,
        string name)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        switch (descriptor)
        {
            case IObjectTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IInterfaceTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IUnionTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IInputObjectTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IEnumTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IObjectFieldDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IInterfaceFieldDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IInputFieldDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IArgumentDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case IDirectiveArgumentDescriptor desc:
                var extend = desc.Extend();
                extend.Definition.AddDirective(
                    new Tag(name),
                    extend.Context.TypeInspector);
                extend.Definition.Dependencies.Add(
                    new TypeDependency(
                        extend.Context.TypeInspector.GetTypeRef(typeof(Tag)),
                        TypeDependencyFulfilled.Completed));
                break;

            case IEnumValueDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            case ISchemaTypeDescriptor desc:
                desc.Directive(new Tag(name));
                break;

            default:
                throw new NotSupportedException(
                    TypeResources.TagDirective_Descriptor_NotSupported);
        }
    }
}
