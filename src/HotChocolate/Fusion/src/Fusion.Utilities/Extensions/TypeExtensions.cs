using HotChocolate.Types;

namespace HotChocolate.Fusion;

public static class TypeExtensions
{
    /// <summary>
    /// Determines whether <paramref name="type"/> is compatible with <paramref name="other"/>
    /// under the rule:
    /// <para>
    ///   <b>At any depth</b> (including nested list elements), <paramref name="type"/>
    ///   may only be as nullable as (or stricter than) <paramref name="other"/>.
    /// </para>
    /// In other words: wherever <paramref name="other"/> is wrapped in <see cref="NonNullType"/>,
    /// <paramref name="type"/> must also be <see cref="NonNullType"/> at the same position.
    /// The list structure and underlying named type must match.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>other: ID</c> → <c>type: ID</c> ✅ or <c>ID!</c> ✅</description>
    ///   </item>
    ///   <item>
    ///     <description><c>other: ID!</c> → <c>type: ID!</c> ✅ (<c>ID</c> ❌)</description>
    ///   </item>
    ///   <item>
    ///     <description><c>other: [ID]</c> → <c>type: [ID]</c> ✅, <c>[ID]!</c> ✅, <c>[ID!]</c> ✅, <c>[ID!]!</c> ✅</description>
    ///   </item>
    ///   <item>
    ///     <description><c>other: [ID!]!</c> → <c>type: [ID!]!</c> ✅ (anything more nullable at any level ❌)</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <param name="type">The candidate type (the one you are checking).</param>
    /// <param name="other">The reference type that defines the maximum allowed nullability.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="type"/> is compatible with <paramref name="other"/> per the rule above; otherwise <c>false</c>.
    /// </returns>
    public static bool IsCompatibleWith(this IType type, IType other)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(other);

        // Work on local variables we can unwrap as we traverse down the shape.
        var t = type;
        var o = other;

        // We iteratively walk both types in lockstep:
        // 1) Enforce the NonNull rule at the current level.
        // 2) Strip NonNull from both sides.
        // 3) If we're at lists, both must be lists; step into element types.
        // 4) If we're at named types, names must match.
        while (true)
        {
            // Rule enforcement: if "other" is NonNull here, "type" must be NonNull too.
            var oIsNonNull = o is NonNullType;
            var tIsNonNull = t is NonNullType;

            if (oIsNonNull && !tIsNonNull)
            {
                // "type" is looser (more nullable) than "other" at this level → incompatible.
                return false;
            }

            // Unwrap any NonNull wrappers so we can compare the inner shape.
            if (oIsNonNull)
            {
                o = ((NonNullType)o).NullableType;
            }

            if (tIsNonNull)
            {
                t = ((NonNullType)t).NullableType;
            }

            // If "other" is a list at this level, "type" must also be a list.
            if (o is ListType oList)
            {
                if (t is not ListType tList)
                {
                    // Shape mismatch: one is a list, the other is not.
                    return false;
                }

                // Dive into element types and continue the loop.
                o = oList.ElementType;
                t = tList.ElementType;
                continue;
            }

            // At this point, both should be named types
            // (e.g., ID, String, Foo, etc.). Names must match.
            if (o is INameProvider oNamed && t is INameProvider tNamed)
            {
                return string.Equals(oNamed.Name, tNamed.Name, StringComparison.Ordinal);
            }

            // Any other combination is a shape mismatch.
            return false;
        }
    }
}
