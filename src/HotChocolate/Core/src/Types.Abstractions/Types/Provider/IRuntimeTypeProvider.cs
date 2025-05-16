
#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// Represents a type system member that manifests as a concrete CLR type at runtime.
/// </summary>
public interface IRuntimeTypeProvider : ITypeSystemMember
{
    /// <summary>
    /// Gets the CLR type that represents the runtime value described by this type system member.
    /// For example, an object type might map to a .NET class, and a field to the return type of its resolver.
    /// </summary>
    Type RuntimeType { get; }
}
