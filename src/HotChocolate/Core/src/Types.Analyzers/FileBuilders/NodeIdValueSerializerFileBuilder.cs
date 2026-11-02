using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class NodeIdValueSerializerFileBuilder : IDisposable
{
    private const string Namespace = "HotChocolate.Execution.Generated";

    private StringBuilder _sb;
    private CodeWriter _writer;
    private bool _disposed;

    public NodeIdValueSerializerFileBuilder()
    {
        _sb = PooledObjects.GetStringBuilder();
        _writer = new(_sb);
    }

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using HotChocolate.Types.Relay;");
        _writer.WriteIndentedLine("using Microsoft.Extensions.DependencyInjection;");
        _writer.WriteLine();
    }

    public void WriteBeginNamespace()
    {
        _writer.WriteIndentedLine("namespace {0}", Namespace);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
        _writer.Write(Properties.SourceGenResources.InterceptsAttribute);
    }

    public void WriteBeginClass()
    {
        _writer.WriteIndentedLine("internal static class NodeIdValueSerializers");
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteSerializer(string serializerName, INamedTypeSymbol compositeId)
    {
        var fullyQualified = compositeId.ToFullyQualified();
        WriteCompositeSerializer(serializerName, fullyQualified, compositeId);
    }

    private void WriteCompositeSerializer(
        string serializerName,
        string fullyQualified,
        INamedTypeSymbol compositeId)
    {
        var properties = GetFormattableProperties(compositeId);

        _writer.WriteIndentedLine(
            "private sealed class {0} : CompositeNodeIdValueSerializer<{1}>",
            serializerName,
            fullyQualified);

        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            WriteFormatMethod(fullyQualified, properties);
            _writer.WriteLine();
            WriteTryParseMethod(fullyQualified, compositeId, properties);
        }
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    private void WriteFormatMethod(
        string fullyQualified,
        List<(string Name, ITypeSymbol Type)> properties)
    {
        _writer.WriteIndentedLine(
            "protected override NodeIdFormatterResult Format(Span<byte> buffer, {0} value, out int written)",
            fullyQualified);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            if (properties.Count == 0)
            {
                _writer.WriteIndentedLine("written = 0;");
                _writer.WriteIndentedLine("return NodeIdFormatterResult.Success;");
            }
            else
            {
                // We build an if-chain that formats each property into the buffer.
                _writer.WriteIndented("if (");

                for (var i = 0; i < properties.Count; i++)
                {
                    var (name, _) = properties[i];
                    var writtenVar = $"{ToCamelCase(name)}Written";
                    var bufferSlice = i == 0
                        ? "buffer"
                        : $"buffer[({string.Join(" + ", Enumerable.Range(0, i).Select(j => $"{ToCamelCase(properties[j].Name)}Written"))})..]";

                    if (i > 0)
                    {
                        _writer.WriteLine();
                        _writer.WriteIndented("    && ");
                    }

                    _writer.Write(
                        "TryFormatIdPart({0}, value.{1}, out var {2})",
                        bufferSlice,
                        name,
                        writtenVar);
                }

                _writer.WriteLine(")");
                _writer.WriteIndentedLine("{");
                using (_writer.IncreaseIndent())
                {
                    var writtenExpression = string.Join(
                        " + ",
                        properties.Select(p => $"{ToCamelCase(p.Name)}Written"));
                    _writer.WriteIndentedLine("written = {0};", writtenExpression);
                    _writer.WriteIndentedLine("return NodeIdFormatterResult.Success;");
                }
                _writer.WriteIndentedLine("}");
                _writer.WriteLine();
                _writer.WriteIndentedLine("written = 0;");
                _writer.WriteIndentedLine("return NodeIdFormatterResult.BufferTooSmall;");
            }
        }
        _writer.WriteIndentedLine("}");
    }

    private void WriteTryParseMethod(
        string fullyQualified,
        INamedTypeSymbol compositeId,
        List<(string Name, ITypeSymbol Type)> properties)
    {
        _writer.WriteIndentedLine(
            "protected override bool TryParse(ReadOnlySpan<byte> buffer, out {0} value)",
            fullyQualified);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            if (properties.Count == 0)
            {
                _writer.WriteIndentedLine("value = default;");
                _writer.WriteIndentedLine("return true;");
            }
            else
            {
                // We build an if-chain that parses each property from the buffer.
                _writer.WriteIndented("if (");

                for (var i = 0; i < properties.Count; i++)
                {
                    var (name, type) = properties[i];
                    var varName = ToCamelCase(name);
                    var typeName = GetCSharpTypeName(type);
                    var isLast = i == properties.Count - 1;
                    var consumedVar = isLast ? "_" : $"{varName}Consumed";
                    var bufferSlice = i == 0
                        ? "buffer"
                        : $"buffer[({string.Join(" + ", Enumerable.Range(0, i).Select(j => $"{ToCamelCase(properties[j].Name)}Consumed"))})..]";

                    if (i > 0)
                    {
                        _writer.WriteLine();
                        _writer.WriteIndented("    && ");
                    }

                    _writer.Write(
                        "TryParseIdPart({0}, out {1} {2}, out var {3})",
                        bufferSlice,
                        typeName,
                        varName,
                        consumedVar);
                }

                _writer.WriteLine(")");
                _writer.WriteIndentedLine("{");
                using (_writer.IncreaseIndent())
                {
                    WriteConstruction(fullyQualified, compositeId, properties);
                    _writer.WriteIndentedLine("return true;");
                }
                _writer.WriteIndentedLine("}");
                _writer.WriteLine();
                _writer.WriteIndentedLine("value = default;");
                _writer.WriteIndentedLine("return false;");
            }
        }
        _writer.WriteIndentedLine("}");
    }

    private void WriteConstruction(
        string fullyQualified,
        INamedTypeSymbol compositeId,
        List<(string Name, ITypeSymbol Type)> properties)
    {
        // We try to construct the value using the best available approach:
        // records and types with matching constructors use positional construction,
        // otherwise we fall back to object initializer syntax.
        if (compositeId.IsRecord || HasMatchingConstructor(compositeId, properties))
        {
            var args = string.Join(", ", properties.Select(p => ToCamelCase(p.Name)));
            _writer.WriteIndentedLine("value = new {0}({1});", fullyQualified, args);
        }
        else
        {
            _writer.WriteIndentedLine("value = new {0}", fullyQualified);
            _writer.WriteIndentedLine("{");
            using (_writer.IncreaseIndent())
            {
                foreach (var (name, _) in properties)
                {
                    _writer.WriteIndentedLine("{0} = {1},", name, ToCamelCase(name));
                }
            }
            _writer.WriteIndentedLine("};");
        }
    }

    public void WriteInterceptMethod(
        int index,
        string serializerName,
        (string FilePath, int LineNumber, int CharacterNumber) location)
    {
        _writer.WriteIndentedLine(
            "[InterceptsLocation(\"{0}\", {1}, {2})]",
            location.FilePath.Replace("\\", "\\\\"),
            location.LineNumber,
            location.CharacterNumber);
        _writer.WriteIndentedLine(
            "public static global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder AddNodeIdValueSerializerFromGen{0}<T>(",
            index);

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "this global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder)");
        }

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "=> builder.AddNodeIdValueSerializer<{0}>();",
                serializerName);
        }

        _writer.WriteLine();
    }

    public override string ToString()
        => _sb.ToString();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        PooledObjects.Return(_sb);
        _sb = null!;
        _writer = null!;
        _disposed = true;
    }

    private static List<(string Name, ITypeSymbol Type)> GetFormattableProperties(
        INamedTypeSymbol compositeId)
    {
        var properties = new List<(string Name, ITypeSymbol Type)>();

        foreach (var member in compositeId.GetMembers())
        {
            if (member is not IPropertySymbol
                {
                    IsStatic: false,
                    DeclaredAccessibility: Accessibility.Public,
                    GetMethod: not null
                } property)
            {
                continue;
            }

            // We skip the EqualityContract property that records generate.
            if (property.Name == "EqualityContract")
            {
                continue;
            }

            if (!IsSupportedType(property.Type))
            {
                continue;
            }

            properties.Add((property.Name, property.Type));
        }

        return properties;
    }

    private static bool IsSupportedType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_Boolean => true,
            _ when type.ToDisplayString() == "System.Guid" => true,
            _ => false
        };
    }

    private static string GetCSharpTypeName(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "string",
            SpecialType.System_Int16 => "short",
            SpecialType.System_Int32 => "int",
            SpecialType.System_Int64 => "long",
            SpecialType.System_Boolean => "bool",
            _ when type.ToDisplayString() == "System.Guid" => "System.Guid",
            _ => type.ToFullyQualified()
        };
    }

    private static bool HasMatchingConstructor(
        INamedTypeSymbol type,
        List<(string Name, ITypeSymbol Type)> properties)
    {
        foreach (var ctor in type.Constructors)
        {
            if (ctor.Parameters.Length != properties.Count)
            {
                continue;
            }

            var allMatch = true;

            for (var i = 0; i < properties.Count; i++)
            {
                var param = ctor.Parameters[i];
                var (name, propType) = properties[i];

                if (!param.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || !SymbolEqualityComparer.Default.Equals(param.Type, propType))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                return true;
            }
        }

        return false;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var result = char.ToLowerInvariant(name[0]) + name.Substring(1);

        // We add a prefix to avoid collisions with method parameters
        // ("value", "buffer", "written") and C# keywords.
        return result switch
        {
            "value" or "buffer" or "written" => $"p_{result}",
            _ => result
        };
    }
}
