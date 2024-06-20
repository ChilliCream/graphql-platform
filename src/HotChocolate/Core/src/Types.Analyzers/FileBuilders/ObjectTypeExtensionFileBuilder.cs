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

                        if (resolver.IsPure)
                        {
                            _writer.WriteIndentedLine(
                                ".Extend().Definition.PureResolver = {0}Resolvers.{1}_{2};",
                                objectTypeExtension.Type.ToDisplayString(),
                                objectTypeExtension.Type.Name,
                                resolver.Member.Name);
                        }
                        else
                        {
                            _writer.WriteIndentedLine(
                                ".Extend().Definition.Resolver = {0}Resolvers.{1}_{2};",
                                objectTypeExtension.Type.ToDisplayString(),
                                objectTypeExtension.Type.Name,
                                resolver.Member.Name);
                        }
                    }
                }
            }

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");

            if (objectTypeExtension.Resolvers.Length > 0)
            {
                _writer.WriteLine();
                _writer.WriteIndentedLine("descriptor.Extend().Context.OnSchemaCreated(");
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("schema =>");
                    _writer.WriteIndentedLine("{");
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("var services = schema.Services.GetApplicationServices();");
                        _writer.WriteIndentedLine(
                            "var bindingResolver = services.GetRequiredService<global::{0}>();",
                            WellKnownTypes.ParameterBindingResolver);
                        _writer.WriteIndentedLine(
                            "global::{0}Resolvers.InitializeBindings(bindingResolver);",
                            objectTypeExtension.Type.ToDisplayString());
                    }
                    _writer.WriteIndentedLine("});");
                }
            }
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
