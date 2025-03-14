using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class NodeIdValueSerializerGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        string assemblyName,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is not NodeIdValueSerializerInfo compositeNodeIdSerializer)
            {
                continue;
            }

            using var codeFile = CodeFile.Create(
                compositeNodeIdSerializer.CompositeId.ContainingNamespace.ToDisplayString(),
                $"{compositeNodeIdSerializer.CompositeId.Name}NodeIdValueSerializer.g.cs");

            GenerateNodeIdSerializer(
                compositeNodeIdSerializer.CompositeId,
                codeFile.Writer);

            context.AddSource(codeFile.FullName, codeFile.ToString());
        }

         GenerateInterceptor(context, syntaxInfos);
    }

    private static void GenerateNodeIdSerializer(
        INamedTypeSymbol compositeId,
        CodeWriter sourceFile)
    {
        var typeName = compositeId.Name;
        var serializerName = $"{typeName}NodeIdValueSerializer";

        sourceFile.WriteIndentedLine($"private sealed class {serializerName} : CompositeNodeIdValueSerializer<{typeName}>");
        using (sourceFile.WithCurlyBrace())
        {
            using (sourceFile.WriteMethod("protected override", "NodeIdFormatterResult", "Format", $"Span<byte> buffer", $"{typeName} value", "out int written"))
            {
                sourceFile.WriteIndentedLine("int offset = 0;");

                foreach (var member in compositeId.GetMembers().OfType<IPropertySymbol>().Where(m => !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && m.GetMethod != null))
                {
                    var formatMethod = GetFormatMethod(member.Type);
                    if (formatMethod != null)
                    {
                        sourceFile.WriteIndentedLine($"if (!{formatMethod}(buffer.Slice(offset), value.{member.Name}, out var {member.Name.ToLower()}Written))");
                        using (sourceFile.WithCurlyBrace())
                        {
                            sourceFile.WriteIndentedLine("return NodeIdFormatterResult.BufferTooSmall;");
                        }
                        sourceFile.WriteIndentedLine($"offset += {member.Name.ToLower()}Written;");
                    }
                }

                sourceFile.WriteIndentedLine("written = offset;");
                sourceFile.WriteIndentedLine("return NodeIdFormatterResult.Success;");
            }

            // Generate TryParse method
            using (sourceFile.WriteMethod("protected override", "bool", "TryParse", "ReadOnlySpan<byte> buffer", $"out {typeName} value"))
            {
                sourceFile.WriteIndentedLine("int offset = 0;");

                var parsedValues = new List<string>();

                foreach (var member in compositeId.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public ||
                        (member.SetMethod == null
                            && !compositeId.IsRecord
                            && !HasConstructorParameter(compositeId, member.Name)))
                    {
                        continue;
                    }

                    var parseMethod = GetParseMethod(member.Type);
                    if (parseMethod != null)
                    {
                        string varName = member.Name.ToLower();
                        sourceFile.WriteIndentedLine($"if (!{parseMethod}(buffer.Slice(offset), out var {varName}, out var {varName}Consumed))");
                        using (sourceFile.WithCurlyBrace())
                        {
                            sourceFile.WriteIndentedLine("value = default;");
                            sourceFile.WriteIndentedLine("return false;");
                        }
                        sourceFile.WriteIndentedLine($"offset += {varName}Consumed;");
                        parsedValues.Add(varName);
                    }
                }

                if (compositeId.IsRecord || HasConstructorParameters(compositeId))
                {
                    sourceFile.WriteIndentedLine($"value = new {typeName}({string.Join(", ", parsedValues)});");
                }
                else
                {
                    sourceFile.WriteIndentedLine($"value = new {typeName}()");
                    using (sourceFile.WithCurlyBrace())
                    {
                        foreach (var member in compositeId.GetMembers().OfType<IPropertySymbol>().Where(m => !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && (m.SetMethod != null || compositeId.IsRecord)))
                        {
                            sourceFile.WriteIndentedLine($"{member.Name} = {member.Name.ToLower()},");
                        }
                    }
                    sourceFile.WriteIndentedLine(";");
                }
                sourceFile.WriteIndentedLine("return true;");
            }
        }
    }

    private static void GenerateInterceptor(
        SourceProductionContext context,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        using var codeFile = CodeFile.Create(
            "HotChocolate.Execution.Generated",
            "HotChocolateNodeIdValueSerializers");

        codeFile.Writer.WriteIndentedLine("namespace HotChocolate.Execution.Generated;");
        codeFile.Writer.WriteLine();
        codeFile.Writer.WriteIndentedLine("public static class HotChocolateNodeIdValueSerializers");
        using (codeFile.Writer.WithCurlyBrace())
        {
            foreach (var syntaxInfo in syntaxInfos)
            {
                if (syntaxInfo is not NodeIdValueSerializerInfo compositeNodeIdSerializer)
                {
                    continue;
                }

                var typeName = compositeNodeIdSerializer.CompositeId.Name;
                var serializerName = $"{typeName}NodeIdValueSerializer";

                codeFile.Writer.WriteIndentedLine("[InterceptsLocation(\"HotChocolate.Types.Relay.IRequestExecutorBuilder\", \"AddNodeIdValueSerializerFrom\")]");
                using (codeFile.Writer.WriteMethod(
                    "public static",
                    "IRequestExecutorBuilder",
                    "AddNodeIdValueSerializerFromOurType",
                    "this IRequestExecutorBuilder builder"))
                {
                    codeFile.Writer.WriteIndentedLine($"return RequestExecutorBuilderExtensions.AddNodeIdValueSerializer<{serializerName}>(builder);");
                }
            }
        }

        context.AddSource(codeFile.FullName, codeFile.ToString());
    }

    private static bool HasConstructorParameter(INamedTypeSymbol typeSymbol, string propertyName)
    {
        return typeSymbol.Constructors.Any(c => c.Parameters.Any(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasConstructorParameters(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.Constructors.Any(c => c.Parameters.Length > 0);
    }

    private static string? GetFormatMethod(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "TryFormatIdPart",
            SpecialType.System_Int32 => "TryFormatIdPart",
            SpecialType.System_Int16 => "TryFormatIdPart",
            SpecialType.System_Int64 => "TryFormatIdPart",
            SpecialType.System_Boolean => "TryFormatIdPart",
            _ when type.ToString() == "System.Guid" => "TryFormatIdPart",
            _ => null
        };
    }

    private static string? GetParseMethod(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "TryParseIdPart",
            SpecialType.System_Int32 => "TryParseIdPart",
            SpecialType.System_Int16 => "TryParseIdPart",
            SpecialType.System_Int64 => "TryParseIdPart",
            SpecialType.System_Boolean => "TryParseIdPart",
            _ when type.ToString() == "System.Guid" => "TryParseIdPart",
            _ => null
        };
    }
}
