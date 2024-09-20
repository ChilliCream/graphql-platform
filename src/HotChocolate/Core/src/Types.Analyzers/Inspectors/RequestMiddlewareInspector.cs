using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

internal sealed class RequestMiddlewareInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [MiddlewareMethod.Instance];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: "UseRequest",
                        TypeArgumentList: { Arguments.Count: 1, } args,
                    },
                },
            } node)
        {
            var semanticModel = context.SemanticModel;
            var middlewareType = semanticModel.GetTypeInfo(args.Arguments[0]).Type;

            if (middlewareType is null)
            {
                syntaxInfo = default;
                return false;
            }

            var methods = middlewareType.GetMembers().OfType<IMethodSymbol>().ToArray();
            var ctor = methods.FirstOrDefault(t => t.Name.Equals(".ctor"));
            var invokeMethod = methods.FirstOrDefault(t => t.Name.Equals("InvokeAsync") || t.Name.Equals("Invoke"));

            if (invokeMethod is not
                {
                    Kind: SymbolKind.Method,
                    IsStatic: false,
                    IsAbstract: false,
                    DeclaredAccessibility: Accessibility.Public,
                } ||
                ctor is not
                {
                    Kind: SymbolKind.Method,
                    IsStatic: false,
                    IsAbstract: false,
                    DeclaredAccessibility: Accessibility.Public,
                })
            {
                syntaxInfo = default;
                return false;
            }

            var ctorParameters = new List<RequestMiddlewareParameterInfo>();
            var invokeParameters = new List<RequestMiddlewareParameterInfo>();

            foreach (var parameter in ctor.Parameters)
            {
                RequestMiddlewareParameterKind kind;
                var parameterTypeName = parameter.Type.ToFullyQualified();

                if (parameterTypeName.Equals("global::HotChocolate.Schema") ||
                    parameterTypeName.Equals("global::HotChocolate.!Schema"))
                {
                    kind = RequestMiddlewareParameterKind.Schema;
                }
                else if (parameterTypeName.Equals("global::HotChocolate.Execution.RequestDelegate"))
                {
                    kind = RequestMiddlewareParameterKind.Next;
                }
                else if (HasAttribute(parameter.GetAttributes(), "global::HotChocolate.SchemaServiceAttribute"))
                {
                    kind = RequestMiddlewareParameterKind.SchemaService;
                }
                else
                {
                    kind = RequestMiddlewareParameterKind.Service;
                }

                ctorParameters.Add(new RequestMiddlewareParameterInfo(kind, parameterTypeName));
            }

            foreach (var parameter in invokeMethod.Parameters)
            {
                RequestMiddlewareParameterKind kind;
                var parameterTypeName = parameter.Type.ToFullyQualified();

                if (parameterTypeName.Equals("global::HotChocolate.Schema") ||
                    parameterTypeName.Equals("global::HotChocolate.ISchema"))
                {
                    kind = RequestMiddlewareParameterKind.Schema;
                }
                else if (parameterTypeName.Equals("global::HotChocolate.Execution.RequestDelegate"))
                {
                    kind = RequestMiddlewareParameterKind.Next;
                }
                else if (parameterTypeName.Equals("global::HotChocolate.Execution.IRequestContext"))
                {
                    kind = RequestMiddlewareParameterKind.Context;
                }
                else
                {
                    kind = RequestMiddlewareParameterKind.Service;
                }

                invokeParameters.Add(new RequestMiddlewareParameterInfo(kind, parameterTypeName));
            }

            syntaxInfo = new RequestMiddlewareInfo(
                middlewareType.Name,
                middlewareType.ToFullyQualified(),
                invokeMethod.Name,
                GetLocation((MemberAccessExpressionSyntax)node.Expression, semanticModel),
                ctorParameters,
                invokeParameters);
            return true;
        }

        syntaxInfo = default;
        return false;
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, string attributeTypeName)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.ToFullyQualified().Equals(attributeTypeName) ?? false)
            {
                return true;
            }
        }

        return false;
    }

    private static (string, int, int) GetLocation(
        MemberAccessExpressionSyntax memberAccessorExpression,
        SemanticModel semanticModel)
    {
        var invocationNameSpan = memberAccessorExpression.Name.Span;
        var lineSpan = memberAccessorExpression.SyntaxTree.GetLineSpan(invocationNameSpan);
        var filePath = GetInterceptorFilePath(
            memberAccessorExpression.SyntaxTree,
            semanticModel.Compilation.Options.SourceReferenceResolver);
        return (filePath, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1);
    }

    private static string GetInterceptorFilePath(SyntaxTree tree, SourceReferenceResolver? resolver) =>
        resolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
}
