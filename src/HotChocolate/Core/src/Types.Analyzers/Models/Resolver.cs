using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class Resolver
{
    private readonly IMemberDescription? _description;

    public Resolver(
        string typeName,
        ISymbol member,
        IMemberDescription? description,
        string? deprecationReason,
        ResolverResultKind resultKind,
        ImmutableArray<ResolverParameter> parameters,
        ImmutableArray<MemberBinding> bindings,
        SchemaTypeReference schemaTypeRef,
        ResolverKind kind = ResolverKind.Default,
        FieldFlags flags = FieldFlags.None)
    {
        TypeName = typeName;
        Member = member;
        _description = description;
        DeprecationReason = deprecationReason;
        FieldName = member.GetName();
        SchemaTypeRef = schemaTypeRef;
        ResultKind = resultKind;
        Parameters = parameters;
        Bindings = bindings;
        Kind = kind;
        Flags = flags;

        if (description is MethodDescription m && parameters.Length == m.ParameterDescriptions.Length)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                parameters[i].Description ??= m.ParameterDescriptions[i];
            }
        }

        var attributes = member.GetAttributes();
        Shareable = attributes.GetShareableScope();
        Inaccessible = attributes.GetInaccessibleScope();
        IsNodeResolver = attributes.IsNodeResolver();
        Attributes = attributes.GetUserAttributes();
    }

    public string FieldName { get; }

    public string TypeName { get; }

    public string? Description => _description?.Description;

    public string? DeprecationReason { get; }

    public ISymbol Member { get; }

    public ITypeSymbol ReturnType
        => Member.GetReturnType()
            ?? throw new InvalidOperationException("Resolver has no return type.");

    public SchemaTypeReference SchemaTypeRef { get; }

    public bool IsStatic => Member.IsStatic;

    public bool IsPure
        => Kind is not ResolverKind.NodeResolver
            && ResultKind is ResolverResultKind.Pure
            && Parameters.All(t => t.IsPure);

    public ResolverKind Kind { get; }

    public FieldFlags Flags { get; }

    public ResolverResultKind ResultKind { get; }

    public ImmutableArray<ResolverParameter> Parameters { get; }

    public bool RequiresParameterBindings => Parameters.Any(t => t.RequiresBinding);

    public ImmutableArray<MemberBinding> Bindings { get; }

    public bool IsNodeResolver { get; }

    public DirectiveScope Shareable { get; }

    public DirectiveScope Inaccessible { get; }

    public ImmutableArray<AttributeData> Attributes { get; }

    public Resolver WithSchemaTypeName(SchemaTypeReference schemaTypeRef)
        => new Resolver(
            TypeName,
            Member,
            _description,
            DeprecationReason,
            ResultKind,
            Parameters,
            Bindings,
            schemaTypeRef,
            Kind,
            Flags);
}
