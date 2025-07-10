using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ConnectionTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteBeginClass(IOutputTypeInfo type)
    {
        Writer.WriteIndentedLine(
            "{0} partial class {1} : ObjectType<global::{2}>",
            type.IsPublic ? "public" : "internal",
            type.Name,
            type.RuntimeTypeFullName);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not ConnectionTypeInfo connectionType)
        {
            throw new InvalidOperationException(
                "The specified type is not a connection type.");
        }

        Writer.WriteIndentedLine(
            "protected override void Configure(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IObjectTypeDescriptor,
            connectionType.RuntimeTypeFullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            if (connectionType.Resolvers.Length > 0)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof(global::{0});",
                    connectionType.RuntimeTypeFullName);
                Writer.WriteIndentedLine(
                    "var extend = descriptor.Extend();");
                Writer.WriteIndentedLine(
                    "var bindingResolver = extend.Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    connectionType.Resolvers.Any(t => t.RequiresParameterBindings)
                        ? "var resolvers = new __Resolvers(bindingResolver);"
                        : "var resolvers = new __Resolvers();");
            }

            if (connectionType.RuntimeType.IsGenericType
                && !string.IsNullOrEmpty(connectionType.NameFormat)
                && connectionType.NameFormat!.Contains("{0}"))
            {
                var nodeTypeName = connectionType.RuntimeType.TypeArguments[0].ToFullyQualified();
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "var nodeTypeRef = extend.Context.TypeInspector.GetTypeRef(typeof({0}));",
                    nodeTypeName);
                Writer.WriteIndentedLine("descriptor");
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(
                        ".Name(t => string.Format(\"{0}\", t.Name))",
                        connectionType.NameFormat);
                    Writer.WriteIndentedLine(
                        ".DependsOn(nodeTypeRef);");
                }
            }
            else if (!string.IsNullOrEmpty(connectionType.NameFormat))
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "descriptor.Name(\"{0}\");",
                    connectionType.NameFormat);
            }

            WriteResolverBindings(connectionType);
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    public override void WriteConfigureMethod(IOutputTypeInfo type)
    {
    }

    protected override void WriteResolverBindingDescriptor(IOutputTypeInfo type, Resolver resolver)
    {
        if (type is not ConnectionTypeInfo connectionType)
        {
            throw new InvalidOperationException(
                "The specified type is not a connection type.");
        }

        if ((resolver.Flags & FieldFlags.ConnectionEdgesField) == FieldFlags.ConnectionEdgesField)
        {
            var edgeTypeName = $"{connectionType.Namespace}.{connectionType.EdgeTypeName}";

            Writer.WriteIndentedLine(
                ".Type<{0}>()",
                ToGraphQLType(resolver.UnwrappedReturnType, edgeTypeName));
        }
        else
        {
            base.WriteResolverBindingDescriptor(type, resolver);
        }
    }

    private static string ToGraphQLType(
        ITypeSymbol typeSymbol,
        string edgeTypeName)
    {
        var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var innerType = GetListInnerType(namedTypeSymbol);

            if (innerType is null)
            {
                return isNullable
                    ? $"global::{edgeTypeName}"
                    : $"global::{WellKnownTypes.NonNullType}<global::{edgeTypeName}>";
            }

            var type = ToGraphQLType(innerType, edgeTypeName);
            type = $"global::{WellKnownTypes.ListType}<{type}>";
            return isNullable ? type : $"global::{WellKnownTypes.NonNullType}<{type}>";
        }

        throw new InvalidOperationException($"Unsupported type: {typeSymbol}");
    }

    private static INamedTypeSymbol? GetListInnerType(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.IsGenericType &&
            (typeSymbol.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>" ||
             typeSymbol.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IList<T>"))
        {
            return typeSymbol.TypeArguments[0] as INamedTypeSymbol;
        }

        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            if (interfaceType.IsGenericType &&
                (interfaceType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>" ||
                 interfaceType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IList<T>"))
            {
                return interfaceType.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }
}
