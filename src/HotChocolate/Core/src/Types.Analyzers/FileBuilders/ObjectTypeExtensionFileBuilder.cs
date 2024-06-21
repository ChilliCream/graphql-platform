using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ObjectTypeExtensionFileBuilder(StringBuilder sb, string ns)
{
    private readonly CodeWriter _writer = new(sb);

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using Microsoft.Extensions.DependencyInjection;");
        _writer.WriteLine();
    }

    public void WriteBeginNamespace()
    {
        _writer.WriteIndentedLine("namespace {0}", ns);
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
            if (objectTypeExtension.Resolvers.Length > 0)
            {
                _writer.WriteIndentedLine("const global::{0} bindingFlags =", WellKnownTypes.BindingFlags);
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("global::{0}.Public", WellKnownTypes.BindingFlags);
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("| global::{0}.NonPublic", WellKnownTypes.BindingFlags);
                        _writer.WriteIndentedLine("| global::{0}.Static;", WellKnownTypes.BindingFlags);
                    }
                }

                _writer.WriteLine();

                _writer.WriteIndentedLine(
                    "var thisType = typeof({0});",
                    objectTypeExtension.Type.ToFullyQualified());
                _writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                _writer.WriteIndentedLine(
                    "global::{0}Resolvers.InitializeBindings(bindingResolver);",
                    objectTypeExtension.Type.ToDisplayString());
            }

            if (objectTypeExtension.NodeResolver is not null)
            {
                _writer.WriteLine();
                _writer.WriteIndentedLine("descriptor");
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine(".ImplementsNode()");
                    _writer.WriteIndentedLine(
                        ".ResolveNode({0}Resolvers.{1}_{2}().Resolver);",
                        objectTypeExtension.Type.ToDisplayString(),
                        objectTypeExtension.Type.Name,
                        objectTypeExtension.NodeResolver.Member.Name);
                }
            }

            if (objectTypeExtension.Resolvers.Length > 0)
            {
                foreach (var resolver in objectTypeExtension.Resolvers)
                {
                    _writer.WriteLine();
                    _writer.WriteIndentedLine("descriptor");

                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine(
                            ".Field(thisType.GetMember(\"{0}\", bindingFlags)[0])",
                            resolver.Member.Name);

                        _writer.WriteIndentedLine(
                            ".Extend().Definition.Resolvers = {0}Resolvers.{1}_{2}();",
                            objectTypeExtension.Type.ToDisplayString(),
                            objectTypeExtension.Type.Name,
                            resolver.Member.Name);
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
