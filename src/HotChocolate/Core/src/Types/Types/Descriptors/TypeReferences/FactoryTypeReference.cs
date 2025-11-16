namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The factory type allows the source generator to provide a simple
/// <see cref="TypeReference"/> to the type initialization to construct the
/// GraphQL type definition plus provide a factory that constructs the actual
/// type structure for the field, argument etc.
/// The typeKey is a constant that the source generator produced so that a
/// specific type structure like a lost of string is only produced ones.
/// </summary>
public sealed class FactoryTypeReference : TypeReference
{
    /// <summary>
    /// Initializes a new instance of <see cref="FactoryTypeReference"/>.
    /// </summary>
    /// <param name="typeDefinition">
    /// The type reference that represents the actual type definition.
    /// </param>
    /// <param name="factory">
    /// The factory to create the actual type structure required for a type system member.
    /// </param>
    /// <param name="typeKey">
    /// A key used to express uniqueness.
    /// </param>
    /// <param name="context">
    ///
    /// </param>
    public FactoryTypeReference(
        ExtendedTypeReference typeDefinition,
        Func<IDescriptorContext, ITypeDefinition, IType> factory,
        string typeKey,
        TypeContext context)
        : base(TypeReferenceKind.SourceGeneratorFactory, context, null)
    {
        ArgumentNullException.ThrowIfNull(typeDefinition);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeKey);

        TypeDefinition = typeDefinition;
        Factory = factory;
        Key = typeKey;
    }

    public ExtendedTypeReference TypeDefinition { get; }

    public Func<IDescriptorContext, ITypeDefinition, IType> Factory { get; }

    public string Key { get; }

    public override bool Equals(TypeReference? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is not FactoryTypeReference typeRef)
        {
            return false;
        }

        return Key.Equals(typeRef.Key, StringComparison.Ordinal)
            && typeRef.TypeDefinition.Equals(TypeDefinition);
    }

    public override int GetHashCode()
        => HashCode.Combine(Key, TypeDefinition);
}
