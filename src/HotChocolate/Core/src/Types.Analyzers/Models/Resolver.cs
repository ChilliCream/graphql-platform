using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class Resolver
{
    public Resolver(
        string typeName,
        ISymbol member,
        ResolverResultKind resultKind,
        ImmutableArray<ResolverParameter> parameters,
        ImmutableArray<MemberBinding> bindings,
        string schemaTypeName,
        ResolverKind kind = ResolverKind.Default,
        FieldFlags flags = FieldFlags.None)
    {
        FieldName = member.GetName();
        TypeName = typeName;
        Member = member;
        SchemaTypeName = schemaTypeName;
        ResultKind = resultKind;
        Parameters = parameters;
        Bindings = bindings;
        Kind = kind;
        Flags = flags;

        switch (member)
        {
            case IPropertySymbol property:
                Description = property.GetDescription();
                break;

            case IMethodSymbol method:
                var description = method.GetDescription();
                Description = description.Description;
                if (description.ParameterDescriptions.Length == parameters.Length)
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        parameters[i].Description = description.ParameterDescriptions[i];
                    }
                }
                break;
        }

        DeprecationReason = null;

        var attributes = member.GetAttributes();
        Shareable = attributes.GetShareableScope();
        Inaccessible = attributes.GetInaccessibleScope();
        Attributes = attributes.GetUserAttributes();
    }

    public string FieldName { get; }

    public string TypeName { get; }

    public string? Description { get; }

    public string? DeprecationReason { get; }

    public ISymbol Member { get; }

    public ITypeSymbol ReturnType
        => Member.GetReturnType()
            ?? throw new InvalidOperationException("Resolver has no return type.");

    public string SchemaTypeName { get; }

    public bool IsStatic => Member.IsStatic;

    public bool IsPure
        => Kind is not ResolverKind.NodeResolver
            && ResultKind is ResolverResultKind.Pure
            && Parameters.All(t => t.IsPure);

    public bool IsShareable { get; set; }

    public bool IsInaccessible { get; set; }

    public ResolverKind Kind { get; }

    public FieldFlags Flags { get; }

    public ResolverResultKind ResultKind { get; }

    public ImmutableArray<ResolverParameter> Parameters { get; }

    public bool RequiresParameterBindings => Parameters.Any(t => t.RequiresBinding);

    public ImmutableArray<MemberBinding> Bindings { get; }

    public DirectiveScope Shareable { get; }

    public DirectiveScope Inaccessible { get; }

    public ImmutableArray<AttributeData> Attributes { get; }

    public Resolver WithSchemaTypeName(string schemaTypeName)
    {
        return new Resolver(
            TypeName,
            Member,
            ResultKind,
            Parameters,
            Bindings,
            schemaTypeName,
            Kind,
            Flags);
    }
}
