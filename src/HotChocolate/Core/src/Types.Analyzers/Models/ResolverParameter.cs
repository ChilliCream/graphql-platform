using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class ResolverParameter
{
    public ResolverParameter(
        IParameterSymbol parameter,
        ResolverParameterKind kind,
        SchemaTypeReference schemaTypeRef,
        string? description,
        string? deprecationReason,
        string? key)
        : this(parameter, kind, schemaTypeRef, deprecationReason, key)
    {
        Description = description;
    }

    public ResolverParameter(
        IParameterSymbol parameter,
        ResolverParameterKind kind,
        SchemaTypeReference schemaTypeRef,
        string? deprecationReason,
        string? key)
    {
        Parameter = parameter;
        Kind = kind;
        Name = parameter.Name;
        SchemaTypeRef = schemaTypeRef;
        Key = key;
        IsNullable = !parameter.IsNonNullable();
        DeprecationReason = deprecationReason;
        Attributes = parameter.GetAttributes();

        // if this is the parent attribute we will check if we have requirements for the parent model.
        var parentAttribute = Attributes.FirstOrDefault(a => a.AttributeClass?.Name is "ParentAttribute" or "Parent");
        if (parentAttribute?.ConstructorArguments.Length > 0)
        {
            var requiresArg = parentAttribute.ConstructorArguments[0];
            Requirements = requiresArg.Value as string;
        }

        DescriptorAttributes = Attributes.GetUserAttributes();
    }

    public string Name { get; }

    public string? Description { get; set; }

    public string? DeprecationReason { get; }

    public string? Key { get; }

    public ITypeSymbol Type => Parameter.Type;

    public SchemaTypeReference SchemaTypeRef { get; }

    public ImmutableArray<ITypeSymbol> TypeParameters
        => GetGenericTypeArgument(Type);

    public IParameterSymbol Parameter { get; }

    public ResolverParameterKind Kind { get; }

    public ImmutableArray<AttributeData> Attributes { get; }

    public ImmutableArray<AttributeData> DescriptorAttributes { get; }

    public string? Requirements { get; }

    public bool IsPure
        => Kind is ResolverParameterKind.Argument or
            ResolverParameterKind.Parent or
            ResolverParameterKind.Service or
            ResolverParameterKind.GetGlobalState or
            ResolverParameterKind.SetGlobalState or
            ResolverParameterKind.GetScopedState or
            ResolverParameterKind.HttpContext or
            ResolverParameterKind.HttpRequest or
            ResolverParameterKind.HttpResponse or
            ResolverParameterKind.DocumentNode or
            ResolverParameterKind.EventMessage or
            ResolverParameterKind.FieldNode or
            ResolverParameterKind.OutputField or
            ResolverParameterKind.ClaimsPrincipal or
            ResolverParameterKind.ConnectionFlags;

    public bool RequiresBinding
        => Kind == ResolverParameterKind.Unknown;

    public bool HasConfiguration => Attributes.Length > 0;

    public bool IsNullable { get; }

    public ResolverParameter WithKind(ResolverParameterKind kind)
        => new ResolverParameter(
            Parameter,
            kind,
            SchemaTypeRef,
            Description,
            DeprecationReason,
            Key);

    private static ImmutableArray<ITypeSymbol> GetGenericTypeArgument(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
        {
            return namedTypeSymbol.TypeArguments;
        }

        // Return null if it's not a generic type or index is out of bounds
        return [];
    }
}
