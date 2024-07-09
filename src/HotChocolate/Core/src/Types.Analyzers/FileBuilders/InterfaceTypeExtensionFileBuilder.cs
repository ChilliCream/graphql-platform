using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class InterfaceTypeExtensionFileBuilder(StringBuilder sb, string ns) : IOutputTypeFileBuilder
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

    public void WriteInitializeMethod(IOutputTypeInfo typeInfo)
    {
        _writer.WriteIndentedLine(
            "internal static void Initialize(global::HotChocolate.Types.IInterfaceTypeDescriptor<{0}> descriptor)",
            typeInfo.RuntimeType.ToFullyQualified());
        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            if (typeInfo.Resolvers.Length > 0)
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
                    typeInfo.Type.ToFullyQualified());
                _writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                _writer.WriteIndentedLine(
                    "global::{0}Resolvers.InitializeBindings(bindingResolver);",
                    typeInfo.Type.ToDisplayString());
            }

            if (typeInfo.Resolvers.Length > 0)
            {
                foreach (var resolver in typeInfo.Resolvers)
                {
                    _writer.WriteLine();
                    _writer.WriteIndentedLine("descriptor");

                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine(
                            ".Field(thisType.GetMember(\"{0}\", bindingFlags)[0])",
                            resolver.Member.Name);

                        _writer.WriteIndentedLine(".ExtendWith(c =>");
                        _writer.WriteIndentedLine("{");
                        using (_writer.IncreaseIndent())
                        {
                            _writer.WriteIndentedLine("c.Definition.SetSourceGeneratorFlags();");
                            _writer.WriteIndentedLine(
                                "c.Definition.Resolvers = {0}Resolvers.{1}_{2}();",
                                typeInfo.Type.ToFullyQualified(),
                                typeInfo.Type.Name,
                                resolver.Member.Name);

                            if (resolver.ResultKind is not ResolverResultKind.Pure
                                && !resolver.Member.HasPostProcessorAttribute()
                                && resolver.Member.IsListType(out var elementType))
                            {
                                _writer.WriteIndentedLine(
                                    "c.Definition.ResultPostProcessor = global::{0}<{1}>.Default;",
                                    WellKnownTypes.ListPostProcessor,
                                    elementType);
                            }
                        }
                        _writer.WriteIndentedLine("});");
                    }
                }
            }

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteConfigureMethod(IOutputTypeInfo typeInfo)
    {
        _writer.WriteIndentedLine(
            "static partial void Configure(global::HotChocolate.Types.IInterfaceTypeDescriptor<{0}> descriptor);",
            typeInfo.RuntimeType.ToFullyQualified());
    }
}
