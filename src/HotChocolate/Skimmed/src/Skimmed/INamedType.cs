using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public interface INamedType : IType, IHasName, IHasDirectives, IHasContextData, IHasDescription
{
    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(INamedType type) => IsAssignableFrom(type, TypeComparison.Reference);

    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(INamedType type, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (ReferenceEquals(type, this))
        {
            return true;
        }

        if (comparison is TypeComparison.Structural)
        {
            return type.Kind.Equals(Kind) && type.Name.EqualsOrdinal(Name);
        }

        return false;
    }
}
