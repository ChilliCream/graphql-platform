using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ObjectTypeExtensionFileBuilder(StringBuilder sb, string ns) : IOutputTypeFileBuilder
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
        if (typeInfo is not ObjectTypeExtensionInfo objectTypeExtension)
        {
            return;
        }

        _writer.WriteIndentedLine(
            "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor)",
            objectTypeExtension.RuntimeType.ToFullyQualified());
        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            if (objectTypeExtension.Resolvers.Length > 0 || objectTypeExtension.NodeResolver is not null)
            {
                var hasRuntimeBindings = objectTypeExtension.Resolvers.Any(
                    t => t.Bindings.Any(b => b.Kind == MemberBindingKind.Property));

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

                if (hasRuntimeBindings)
                {
                    _writer.WriteIndentedLine("const global::{0} runtimeBindingFlags =", WellKnownTypes.BindingFlags);
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("global::{0}.Public", WellKnownTypes.BindingFlags);
                        using (_writer.IncreaseIndent())
                        {
                            _writer.WriteIndentedLine("| global::{0}.NonPublic", WellKnownTypes.BindingFlags);
                            _writer.WriteIndentedLine("| global::{0}.Instance", WellKnownTypes.BindingFlags);
                            _writer.WriteIndentedLine("| global::{0}.Static;", WellKnownTypes.BindingFlags);
                        }
                    }
                }

                _writer.WriteLine();

                _writer.WriteIndentedLine(
                    "var thisType = typeof({0});",
                    objectTypeExtension.Type.ToFullyQualified());
                if(hasRuntimeBindings)
                {
                    _writer.WriteIndentedLine(
                        "var runtimeType = typeof({0});",
                        objectTypeExtension.RuntimeType.ToFullyQualified());
                }
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
                        ".ResolveNode({0}Resolvers.{1}_{2}().Resolver!);",
                        objectTypeExtension.Type.ToFullyQualified(),
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

                        _writer.WriteIndentedLine(".ExtendWith(c =>");
                        _writer.WriteIndentedLine("{");
                        using (_writer.IncreaseIndent())
                        {
                            _writer.WriteIndentedLine("c.Definition.SetSourceGeneratorFlags();");
                            _writer.WriteIndentedLine(
                                "c.Definition.Resolvers = {0}Resolvers.{1}_{2}();",
                                objectTypeExtension.Type.ToFullyQualified(),
                                objectTypeExtension.Type.Name,
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

                    if (resolver.Bindings.Length > 0)
                    {
                        foreach (var binding in resolver.Bindings)
                        {
                            _writer.WriteLine();
                            _writer.WriteIndentedLine("descriptor");

                            using (_writer.IncreaseIndent())
                            {
                                if (binding.Kind is MemberBindingKind.Property)
                                {
                                    _writer.WriteIndentedLine(
                                        ".Field(runtimeType.GetMember(\"{0}\", runtimeBindingFlags)[0])",
                                        binding.Name);
                                    _writer.WriteIndentedLine(".Ignore();");
                                }
                                else if (binding.Kind is MemberBindingKind.Property)
                                {
                                    _writer.WriteIndentedLine(".Field(\"{0}\")", binding.Name);
                                    _writer.WriteIndentedLine(".Ignore();");
                                }
                            }
                        }
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
            "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor);",
            typeInfo.RuntimeType.ToFullyQualified());
    }
}
