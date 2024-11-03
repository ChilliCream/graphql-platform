using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Helpers;

public static class DataLoaderAttributeHelper
{
    public static AttributeData GetDataLoaderAttribute(
        this IMethodSymbol methodSymbol)
        => methodSymbol.GetAttributes().First(
            t => string.Equals(
                t.AttributeClass?.Name,
                "DataLoaderAttribute",
                StringComparison.Ordinal));

    public static ImmutableHashSet<string> GetDataLoaderGroupKeys(this IMethodSymbol methodSymbol)
    {
        var groupNamesBuilder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        AddGroupNames(groupNamesBuilder, methodSymbol.GetAttributes());
        AddGroupNames(groupNamesBuilder, methodSymbol.ContainingType.GetAttributes());
        return groupNamesBuilder.Count == 0 ? ImmutableHashSet<string>.Empty : groupNamesBuilder.ToImmutable();

        static void AddGroupNames(ImmutableHashSet<string>.Builder builder, IEnumerable<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (IsDataLoaderGroupAttribute(attribute.AttributeClass))
                {
                    var constructorArguments = attribute.ConstructorArguments;
                    if (constructorArguments.Length > 0)
                    {
                        foreach (var arg in constructorArguments[0].Values)
                        {
                            if (arg.Value is string groupName)
                            {
                                builder.Add(groupName);
                            }
                        }
                    }
                }
            }
        }

        static bool IsDataLoaderGroupAttribute(INamedTypeSymbol? attributeClass)
        {
            if (attributeClass == null)
            {
                return false;
            }

            while (attributeClass != null)
            {
                if (attributeClass.Name == "DataLoaderGroupAttribute")
                {
                    return true;
                }

                attributeClass = attributeClass.BaseType;
            }

            return false;
        }
    }

    public static string? GetDataLoaderStateKey(
        this IParameterSymbol parameter)
    {
        foreach (var attributeData in parameter.GetAttributes())
        {
            if (!IsTypeName(attributeData.AttributeClass, "GreenDonut", "DataLoaderStateAttribute"))
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0
                && !attributeData.ConstructorArguments[0].IsNull)
            {
                return attributeData.ConstructorArguments[0].Value?.ToString();
            }

            var keyProperty = attributeData.NamedArguments.FirstOrDefault(kv => kv.Key == "Key").Value;
            if (!keyProperty.IsNull)
            {
                return keyProperty.Value?.ToString();
            }

            return null;
        }

        return null;
    }

    private static bool IsTypeName(INamedTypeSymbol? type, string containingNamespace, string typeName)
    {
        if (type is null)
        {
            return false;
        }

        while (type != null)
        {
            if (type.MetadataName == typeName && type.ContainingNamespace?.ToDisplayString() == containingNamespace)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    public static bool? IsScoped(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        GeneratorSyntaxContext context)
    {
        var argumentSyntax = arguments.FirstOrDefault(
            t => t.NameEquals?.Name.ToFullString().Trim() == "ServiceScope");

        if (argumentSyntax is not null)
        {
            var valueExpression = argumentSyntax.Expression;
            var value = context.SemanticModel.GetConstantValue(valueExpression).Value;

            if (value is not null)
            {
                switch ((int)value)
                {
                    case 0:
                        return null;

                    case 1:
                        return true;

                    case 2:
                        return false;
                }
            }
        }

        return null;
    }

    public static string[] GetLookups(this AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key.Equals("Lookups", StringComparison.Ordinal)
                && !argument.Value.IsNull
                && argument.Value.Values.Any())
            {
                var values = new string[argument.Value.Values.Length];
                for (var i = 0; i < argument.Value.Values.Length; i++)
                {
                    values[i] = (string)argument.Value.Values[i].Value!;
                }

                return values;
            }
        }

        return [];
    }

    public static bool? IsScoped(this AttributeData attribute)
    {
        var scoped = attribute.NamedArguments.FirstOrDefault(
            t => t.Key.Equals("ServiceScope", StringComparison.Ordinal));

        if (!scoped.Value.IsNull)
        {
            switch ((int)scoped.Value.Value!)
            {
                case 0:
                    return null;

                case 1:
                    return true;

                case 2:
                    return false;
            }
        }

        return null;
    }

    public static bool? IsPublic(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        GeneratorSyntaxContext context)
    {
        var argumentSyntax = arguments.FirstOrDefault(
            t => t.NameEquals?.Name.ToFullString().Trim() == "AccessModifier");

        if (argumentSyntax is not null)
        {
            var valueExpression = argumentSyntax.Expression;
            var value = context.SemanticModel.GetConstantValue(valueExpression).Value;

            if (value is not null)
            {
                switch ((int)value)
                {
                    case 0:
                        return null;

                    case 1:
                        return true;

                    case 2:
                        return false;

                    case 3:
                        return false;
                }
            }
        }

        return null;
    }

    public static bool? IsInterfacePublic(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        GeneratorSyntaxContext context)
    {
        var argumentSyntax = arguments.FirstOrDefault(
            t => t.NameEquals?.Name.ToFullString().Trim() == "AccessModifier");

        if (argumentSyntax is not null)
        {
            var valueExpression = argumentSyntax.Expression;
            var value = context.SemanticModel.GetConstantValue(valueExpression).Value;

            if (value is not null)
            {
                switch ((int)value)
                {
                    case 0:
                        return null;

                    case 1:
                        return true;

                    case 2:
                        return true;

                    case 3:
                        return false;
                }
            }
        }

        return null;
    }

    public static bool? IsPublic(this AttributeData attribute)
    {
        var scoped = attribute.NamedArguments.FirstOrDefault(
            t => t.Key.Equals("AccessModifier", StringComparison.Ordinal));

        if (scoped.Value.Value is not null)
        {
            switch ((int)scoped.Value.Value)
            {
                case 0:
                    return null;

                case 1:
                    return true;

                case 2:
                    return false;

                case 3:
                    return false;
            }
        }

        return null;
    }

    public static bool? IsInterfacePublic(this AttributeData attribute)
    {
        var scoped = attribute.NamedArguments.FirstOrDefault(
            t => t.Key.Equals("AccessModifier", StringComparison.Ordinal));

        if (scoped.Value.Value is not null)
        {
            switch ((int)scoped.Value.Value)
            {
                case 0:
                    return null;

                case 1:
                    return true;

                case 2:
                    return true;

                case 3:
                    return false;
            }
        }

        return null;
    }

    public static bool RegisterService(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        GeneratorSyntaxContext context)
    {
        var argumentSyntax = arguments.FirstOrDefault(
            t => t.NameEquals?.Name.ToFullString().Trim() == "GenerateRegistrationCode");

        if (argumentSyntax is not null)
        {
            var valueExpression = argumentSyntax.Expression;
            var value = context.SemanticModel.GetConstantValue(valueExpression).Value;

            if (value is not null)
            {
                return (bool)value;
            }
        }

        return true;
    }

    public static bool GenerateInterfaces(
        this SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        GeneratorSyntaxContext context)
    {
        var argumentSyntax = arguments.FirstOrDefault(
            t => t.NameEquals?.Name.ToFullString().Trim() == "GenerateInterfaces");

        if (argumentSyntax is not null)
        {
            var valueExpression = argumentSyntax.Expression;
            var value = context.SemanticModel.GetConstantValue(valueExpression).Value;

            if (value is not null)
            {
                return (bool)value;
            }
        }

        return true;
    }

    public static bool TryGetName(
        this AttributeData attribute,
        [NotNullWhen(true)] out string? name)
    {
        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string s)
        {
            name = s;
            return true;
        }

        name = null;
        return false;
    }
}
