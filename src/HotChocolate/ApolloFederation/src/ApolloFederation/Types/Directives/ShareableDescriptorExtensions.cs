namespace HotChocolate.ApolloFederation.Types;

public static class ShareableDescriptorExtensions
{
    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple
    /// subgraphs.
    /// If an object is marked as @shareable then all its fields are automatically shareable without the need
    /// for explicitly marking them with @shareable directive. All fields referenced from @key directive are
    /// automatically shareable as well.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!                           # shareable because id is a key field
    ///   name: String                      # non-shareable
    ///   description: String @shareable    # shareable
    /// }
    ///
    /// type Bar @shareable {
    ///   description: String               # shareable because User is marked shareable
    ///   name: String                      # shareable because User is marked shareable
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> Shareable<T>(this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(ShareableDirective.Default);
    }

    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple subgraphs.
    /// If an object is marked as @shareable then all its fields are automatically shareable without the need
    /// for explicitly marking them with @shareable directive. All fields referenced from @key directive are
    /// automatically shareable as well.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!                           # shareable because id is a key field
    ///   name: String                      # non-shareable
    ///   description: String @shareable    # shareable
    /// }
    ///
    /// type Bar @shareable {
    ///   description: String               # shareable because User is marked shareable
    ///   name: String                      # shareable because User is marked shareable
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor Shareable(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(ShareableDirective.Default);
    }

    /// <summary>
    /// Applies @shareable directive which indicates that given object and/or field can be resolved by multiple subgraphs.
    /// If an object is marked as @shareable then all its fields are automatically shareable without the need
    /// for explicitly marking them with @shareable directive. All fields referenced from @key directive are
    /// automatically shareable as well.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!                           # shareable because id is a key field
    ///   name: String                      # non-shareable
    ///   description: String @shareable    # shareable
    /// }
    ///
    /// type Bar @shareable {
    ///   description: String               # shareable because User is marked shareable
    ///   name: String                      # shareable because User is marked shareable
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor Shareable(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(ShareableDirective.Default);
    }
}
