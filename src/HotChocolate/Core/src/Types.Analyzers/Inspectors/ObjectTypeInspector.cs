using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ObjectTypeInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [TypeWithAttribute.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = [SyntaxKind.ClassDeclaration];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        var diagnostics = ImmutableArray<Diagnostic>.Empty;
        var isOperationType = false;

        OperationType? operationType = null;
        if (!IsObjectTypeExtension(context, out var possibleType, out var classSymbol, out var runtimeType))
        {
            if (!IsOperationType(context, out possibleType, out classSymbol, out operationType))
            {
                syntaxInfo = null;
                return false;
            }

            isOperationType = true;
        }

        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.ObjectTypePartialKeywordMissing,
                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
        }

        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.ObjectTypeStaticKeywordMissing,
                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
        }

        var members = classSymbol.GetMembers();
        var resolvers = new Resolver[members.Length];
        Resolver? nodeResolver = null;
        var i = 0;

        foreach (var member in members)
        {
            if (member.DeclaredAccessibility is Accessibility.Public && !member.IsIgnored())
            {
                if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol)
                {
                    if (methodSymbol.Skip())
                    {
                        continue;
                    }

                    if (!isOperationType && methodSymbol.IsNodeResolver())
                    {
                        nodeResolver = CreateNodeResolver(context, classSymbol, methodSymbol, ref diagnostics);
                    }
                    else
                    {
                        resolvers[i++] = CreateResolver(context, classSymbol, methodSymbol);
                        continue;
                    }
                }

                if (member is IPropertySymbol)
                {
                    resolvers[i++] = new Resolver(
                        classSymbol.Name,
                        member,
                        ResolverResultKind.Pure,
                        [],
                        member.GetMemberBindings());
                }
            }
        }

        if (i > 0 && i < resolvers.Length)
        {
            Array.Resize(ref resolvers, i);
        }

        if (runtimeType is not null)
        {
            syntaxInfo = new ObjectTypeInfo(
                classSymbol,
                runtimeType,
                nodeResolver,
                possibleType,
                i == 0
                    ? []
                    : ImmutableCollectionsMarshal.AsImmutableArray(resolvers));

            if (diagnostics.Length > 0)
            {
                syntaxInfo.AddDiagnosticRange(diagnostics);
            }
            return true;
        }

        syntaxInfo = new RootTypeInfo(
            classSymbol,
            operationType!.Value,
            possibleType,
            i == 0
                ? []
                : ImmutableCollectionsMarshal.AsImmutableArray(resolvers));

        if (diagnostics.Length > 0)
        {
            syntaxInfo.AddDiagnosticRange(diagnostics);
        }
        return true;
    }

    private static bool IsObjectTypeExtension(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ClassDeclarationSyntax? resolverTypeSyntax,
        [NotNullWhen(true)] out INamedTypeSymbol? resolverTypeSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? runtimeType)
    {
        if (context.Node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } possibleType)
        {
            foreach (var attributeListSyntax in possibleType.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // We do a start with here to capture the generic and non-generic variant of
                    // the object type extension attribute.
                    if (fullName.StartsWith(ObjectTypeAttribute, Ordinal) &&
                        attributeContainingTypeSymbol.TypeArguments.Length == 1 &&
                        attributeContainingTypeSymbol.TypeArguments[0] is INamedTypeSymbol rt &&
                        ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol rts)
                    {
                        resolverTypeSyntax = possibleType;
                        resolverTypeSymbol = rts;
                        runtimeType = rt;
                        return true;
                    }
                }
            }
        }

        resolverTypeSyntax = null;
        resolverTypeSymbol = null;
        runtimeType = null;
        return false;
    }

    private static bool IsOperationType(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ClassDeclarationSyntax? resolverTypeSyntax,
        [NotNullWhen(true)] out INamedTypeSymbol? resolverTypeSymbol,
        [NotNullWhen(true)] out OperationType? operationType)
    {
        if (context.Node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } possibleType)
        {
            foreach (var attributeListSyntax in possibleType.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName.StartsWith(QueryTypeAttribute, Ordinal) &&
                        ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol rtsq)
                    {
                        resolverTypeSyntax = possibleType;
                        resolverTypeSymbol = rtsq;
                        operationType = OperationType.Query;
                        return true;
                    }

                    if (fullName.StartsWith(MutationTypeAttribute, Ordinal) &&
                        ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol rtsm)
                    {
                        resolverTypeSyntax = possibleType;
                        resolverTypeSymbol = rtsm;
                        operationType = OperationType.Mutation;
                        return true;
                    }

                    if (fullName.StartsWith(SubscriptionTypeAttribute, Ordinal) &&
                        ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol rtss)
                    {
                        resolverTypeSyntax = possibleType;
                        resolverTypeSymbol = rtss;
                        operationType = OperationType.Subscription;
                        return true;
                    }
                }
            }
        }

        resolverTypeSyntax = null;
        resolverTypeSymbol = null;
        operationType = null;
        return false;
    }

    private static Resolver CreateResolver(
        GeneratorSyntaxContext context,
        INamedTypeSymbol resolverType,
        IMethodSymbol resolverMethod)
        => CreateResolver(context.SemanticModel.Compilation, resolverType, resolverMethod);

    public static Resolver CreateResolver(
        Compilation compilation,
        INamedTypeSymbol resolverType,
        IMethodSymbol resolverMethod,
        string? resolverTypeName = null)
    {
        var parameters = resolverMethod.Parameters;
        var resolverParameters = new ResolverParameter[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            resolverParameters[i] = ResolverParameter.Create(parameters[i], compilation);
        }

        resolverTypeName ??= resolverType.Name;

        return new Resolver(
            resolverTypeName,
            resolverMethod,
            resolverMethod.GetResultKind(),
            [.. resolverParameters],
            resolverMethod.GetMemberBindings(),
            kind: compilation.IsConnectionType(resolverMethod.ReturnType)
                ? ResolverKind.ConnectionResolver
                : ResolverKind.Default);
    }

    private static Resolver CreateNodeResolver(
        GeneratorSyntaxContext context,
        INamedTypeSymbol resolverType,
        IMethodSymbol resolverMethod,
        ref ImmutableArray<Diagnostic> diagnostics)
    {
        var compilation = context.SemanticModel.Compilation;
        var parameters = resolverMethod.Parameters;
        var resolverParameters = new ResolverParameter[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = ResolverParameter.Create(parameters[i], compilation);

            if (parameter.Kind == ResolverParameterKind.Argument)
            {
                if (parameter.Name != "id" && parameter.Key != "id")
                {
                    var location = parameters[i].Locations[0];

                    diagnostics = diagnostics.Add(
                        Diagnostic.Create(
                            Errors.InvalidNodeResolverArgumentName,
                            Location.Create(location.SourceTree!, location.SourceSpan)));
                }
            }

            if (parameter.Kind is ResolverParameterKind.Unknown && (parameter.Name == "id" || parameter.Key == "id"))
            {
                parameter = new ResolverParameter(parameter.Parameter, parameter.Key, ResolverParameterKind.Argument);
            }

            resolverParameters[i] = parameter;
        }

        if (resolverParameters.Count(t => t.Kind == ResolverParameterKind.Argument) > 1)
        {
            var location = resolverMethod.Locations[0];

            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.TooManyNodeResolverArguments,
                    Location.Create(location.SourceTree!, location.SourceSpan)));
        }

        return new Resolver(
            resolverType.Name,
            resolverMethod,
            resolverMethod.GetResultKind(),
            resolverParameters.ToImmutableArray(),
            resolverMethod.GetMemberBindings(),
            kind: ResolverKind.NodeResolver);
    }

    public static ImmutableArray<MemberBinding> GetMemberBindings(ISymbol member)
        => member.GetMemberBindings();
}

file static class Extensions
{
    public static bool IsNodeResolver(this IMethodSymbol methodSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(NodeResolverAttribute))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Skip(this IMethodSymbol methodSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass.IsOrInheritsFrom(
                DataLoaderAttribute,
                QueryAttribute,
                MutationAttribute,
                SubscriptionAttribute))
            {
                return true;
            }
        }

        return false;
    }

    public static ImmutableArray<MemberBinding> GetMemberBindings(this ISymbol member)
    {
        var bindings = ImmutableArray.CreateBuilder<MemberBinding>();

        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString().Equals(BindFieldAttribute, Ordinal) ?? false)
            {
                var name = attribute.ConstructorArguments[0].Value?.ToString();

                if (name is not null)
                {
                    bindings.Add(new MemberBinding(name, MemberBindingKind.Field));
                }
            }
            else if (attribute.AttributeClass?.ToDisplayString().Equals(BindMemberAttribute, Ordinal) ?? false)
            {
                var name = attribute.ConstructorArguments[0].Value?.ToString();

                if (name is not null)
                {
                    bindings.Add(new MemberBinding(name, MemberBindingKind.Property));
                }
            }
        }

        return bindings.ToImmutable();
    }
}
