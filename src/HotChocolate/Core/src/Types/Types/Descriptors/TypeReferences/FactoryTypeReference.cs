using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

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
    /// <param name="typeStructure">
    /// The GraphQL type structure created by this factory reference.
    /// </param>
    public FactoryTypeReference(
        ExtendedTypeReference typeDefinition,
        ITypeNode typeStructure)
        : base(TypeReferenceKind.Factory, typeDefinition.Context, null)
    {
        ArgumentNullException.ThrowIfNull(typeDefinition);
        ArgumentNullException.ThrowIfNull(typeStructure);

        TypeDefinition = typeDefinition;
        TypeStructure = typeStructure;
    }

    public ExtendedTypeReference TypeDefinition { get; }

    public ITypeNode TypeStructure { get; }

    [field: AllowNull]
    public string Key
    {
        get
        {
            field ??= TypeStructure.ToString(indented: false);
            return field;
        }
    }

    public IType Create(ITypeDefinition typeDefinition)
    {
        return CreateType(TypeStructure, typeDefinition);

        static IType CreateType(ITypeNode typeNode, ITypeDefinition typeDefinition)
        {
            return typeNode switch
            {
                NonNullTypeNode nnt => new NonNullType(CreateType(nnt.Type, typeDefinition)),
                ListTypeNode lt => new ListType(CreateType(lt.Type, typeDefinition)),
                NamedTypeNode => typeDefinition,
                _ => throw new NotSupportedException()
            };
        }
    }

    public FactoryTypeReference GetElementType()
    {
        var typeStructure = TypeStructure;

        if (typeStructure is NonNullTypeNode nnt)
        {
            typeStructure = nnt.Type;
        }

        if (typeStructure is not ListTypeNode lt)
        {
            throw new InvalidOperationException(
                string.Format(
                    "This type reference `{0}` does not represent a list type and thus has no element type.",
                    TypeStructure.ToString(indented: false)));
        }

        return new FactoryTypeReference(TypeDefinition, lt.Type);
    }

    public FactoryTypeReference WithTypeDefinition(ExtendedTypeReference typeDefinition, string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeDefinition);

        var type = Rewrite(TypeStructure, typeName.EnsureGraphQLName());
        return new FactoryTypeReference(typeDefinition, type);

        static ITypeNode Rewrite(ITypeNode typeNode, string typeName)
        {
            return typeNode switch
            {
                NonNullTypeNode nnt => new NonNullTypeNode(null, (INullableTypeNode)Rewrite(nnt.Type, typeName)),
                ListTypeNode lt => new ListTypeNode(Rewrite(lt.Type, typeName)),
                NamedTypeNode => new NamedTypeNode(typeName),
                _ => throw new NotSupportedException()
            };
        }
    }

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

        if (!IsEqual(other))
        {
            return false;
        }

        return SyntaxComparer.BySyntax.Equals(TypeStructure, typeRef.TypeStructure)
            && typeRef.TypeDefinition.Equals(TypeDefinition);
    }

    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), TypeStructure, TypeDefinition);
}
