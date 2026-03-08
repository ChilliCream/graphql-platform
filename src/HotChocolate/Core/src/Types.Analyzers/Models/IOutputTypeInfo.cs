using System.Collections.Immutable;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Models;

/// <summary>
/// Represents a C# type that represents a GraphQL type.
/// </summary>
public interface IOutputTypeInfo : IDiagnosticsProvider
{
    /// <summary>
    /// Gets the name that the generator shall use to generate the output type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the namespace that the generator shall use to generate the output type.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets the description of the object type.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Defines if the type is a public.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets the schema type symbol.
    /// </summary>
    INamedTypeSymbol? SchemaSchemaType { get; }

    /// <summary>
    /// Gets the full schema type name.
    /// </summary>
    string? SchemaTypeFullName { get; }

    /// <summary>
    /// Specifies if this type info has a schema type.
    /// </summary>
#if NET8_0_OR_GREATER
    [MemberNotNull(nameof(RuntimeTypeFullName), nameof(RuntimeType))]
#endif
    bool HasSchemaType { get; }

    /// <summary>
    /// Gets the runtime type symbol.
    /// </summary>
    INamedTypeSymbol? RuntimeType { get; }

    /// <summary>
    /// Gets the full runtime type name.
    /// </summary>
    string? RuntimeTypeFullName { get; }

    /// <summary>
    /// Specifies if this type info has a runtime type.
    /// </summary>
#if NET8_0_OR_GREATER
    [MemberNotNull(nameof(RuntimeTypeFullName), nameof(RuntimeType))]
#endif
    bool HasRuntimeType { get; }

    /// <summary>
    /// Gets the class declaration if one exists that this type info is based on.
    /// </summary>
    ClassDeclarationSyntax? ClassDeclaration { get; }

    /// <summary>
    /// Gets the resolvers of this type.
    /// </summary>
    ImmutableArray<Resolver> Resolvers { get; }

    /// <summary>
    /// Specifies if the @shareable directive is annotated to this type.
    /// </summary>
    DirectiveScope Shareable { get; }

    /// <summary>
    /// Specifies if the @inaccessible directive is annotated to this type.
    /// </summary>
    DirectiveScope Inaccessible { get; }

    /// <summary>
    /// Gets all attributes on this type.
    /// </summary>
    ImmutableArray<AttributeData> Attributes { get; }

    /// <summary>
    /// Gets descriptor attributes annotated to this type.
    /// </summary>
    ImmutableArray<AttributeData> DescriptorAttributes { get; }

    void ReplaceResolver(Resolver current, Resolver replacement);
}
