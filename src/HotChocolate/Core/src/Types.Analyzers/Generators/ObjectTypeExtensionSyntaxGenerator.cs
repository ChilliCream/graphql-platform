using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class ObjectTypeExtensionSyntaxGenerator
{
    private readonly string _ns;
    private readonly CodeWriter _writer;

    public ObjectTypeExtensionSyntaxGenerator(StringBuilder sb, string ns)
    {
        _ns = ns;
        _writer = new(sb);
    }

    public void WriterHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteLine();
    }

    public void WriteBeginNamespace()
    {
        _writer.WriteIndentedLine("namespace {0}", _ns);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    public string WriteBeginClass(string typeName)
    {
        _writer.WriteIndentedLine("public static partial class {0}", typeName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
        return typeName;
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteInitializeMethod(ObjectTypeExtensionInfo objectTypeExtension)
    {
        _writer.WriteIndentedLine(
            "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor)",
            objectTypeExtension.RuntimeType.ToFullyQualified());
        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine("const global::System.Reflection.BindingFlags bindingFlags =");
            using (_writer.IncreaseIndent())
            {
                _writer.WriteIndentedLine("global::System.Reflection.BindingFlags.Public |");
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("System.Reflection.BindingFlags.NonPublic |");
                    _writer.WriteIndentedLine("System.Reflection.BindingFlags.Static;");
                }
            }

            _writer.WriteIndentedLine(
                "var thisType = typeof({0});",
                objectTypeExtension.Type.ToFullyQualified());

            if (objectTypeExtension.NodeResolver is not null)
            {
                _writer.WriteLine();
                _writer.WriteIndentedLine("descriptor");
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine(".ImplementsNode()");
                    _writer.WriteIndentedLine(
                        ".ResolveNodeWith((global::System.Reflection.MethodInfo)" +
                        "thisType.GetMember(\"{0}\", bindingFlags)[0]);",
                        objectTypeExtension.NodeResolver.Name);
                }
            }

            if (objectTypeExtension.Members.Length > 0)
            {
                _writer.WriteLine();
                foreach (var member in objectTypeExtension.Members)
                {
                    _writer.WriteIndentedLine("descriptor");

                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine(
                            ".Field(thisType.GetMember(\"{0}\", bindingFlags)[0])",
                            member.Name);

                        if (member is IMethodSymbol method &&
                            method.GetResultKind() is not ResolverResultKind.Pure)
                        {
                            _writer.WriteIndentedLine(
                            ".Extend().Definition.Resolver = HotChocolate.Resolvers.Abc.{0}_{1};",
                            objectTypeExtension.Type.Name,
                            member.Name);
                        }
                        else
                        {
                            _writer.WriteIndentedLine(
                            ".Extend().Definition.PureResolver = HotChocolate.Resolvers.Abc.{0}_{1};",
                            objectTypeExtension.Type.Name,
                            member.Name);
                        }




                    }
                }
            }

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteConfigureMethod(ObjectTypeExtensionInfo objectTypeExtension)
    {
        _writer.WriteIndentedLine(
            "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor);",
            objectTypeExtension.RuntimeType.ToFullyQualified());
    }
}
