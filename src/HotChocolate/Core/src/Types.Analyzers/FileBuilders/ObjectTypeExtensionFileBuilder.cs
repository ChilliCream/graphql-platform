using System.Reflection;
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
        if (typeInfo is ObjectTypeExtensionInfo objectTypeExtension)
        {
            WriteObjectTypeInitializeMethod(objectTypeExtension);
        }
        else if (typeInfo is RootTypeExtensionInfo rootTypeExtension)
        {
            WriteRootTypeInitializeMethod(rootTypeExtension);
        }
        else if(typeInfo is ConnectionObjectTypeInfo connectionTypeExtension)
        {
            WriteConnectionTypeInitializeMethod(connectionTypeExtension);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void WriteObjectTypeInitializeMethod(IOutputTypeInfo typeInfo)
    {
        if (typeInfo is not ObjectTypeExtensionInfo objectTypeExtension)
        {
            return;
        }

        if (typeInfo.IsRootType)
        {
            _writer.WriteIndentedLine(
                "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor descriptor)");
        }
        else
        {
            _writer.WriteIndentedLine(
                "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor)",
                objectTypeExtension.RuntimeType.ToFullyQualified());
        }

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
                if (hasRuntimeBindings)
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

            WriteResolverBindings(objectTypeExtension);

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteRootTypeInitializeMethod(IOutputTypeInfo typeInfo)
    {
        if (typeInfo is not RootTypeExtensionInfo rootTypeExtension)
        {
            return;
        }

        _writer.WriteIndentedLine(
            "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor descriptor)");

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            if (rootTypeExtension.Resolvers.Length > 0)
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
                    rootTypeExtension.Type.ToFullyQualified());
                _writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                _writer.WriteIndentedLine(
                    "global::{0}Resolvers.InitializeBindings(bindingResolver);",
                    rootTypeExtension.Type.ToDisplayString());
            }

            WriteResolverBindings(rootTypeExtension);

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");
        }

        _writer.WriteIndentedLine("}");
    }

    public void WriteConnectionTypeInitializeMethod(ConnectionObjectTypeInfo connectionTypeInfo)
    {
        _writer.WriteIndentedLine(
            "internal static void Initialize(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor)",
            connectionTypeInfo.RuntimeType.ToFullyQualified());

        _writer.WriteIndentedLine("{");

        using (_writer.IncreaseIndent())
        {
            if (connectionTypeInfo.Resolvers.Length > 0)
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

                _writer.WriteLine();

                _writer.WriteIndentedLine(
                    "var runtimeType = typeof({0});",
                    connectionTypeInfo.RuntimeType.ToFullyQualified());
                _writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                _writer.WriteIndentedLine(
                    "global::{0}.{1}Resolvers.InitializeBindings(bindingResolver);",
                    connectionTypeInfo.Namespace,
                    connectionTypeInfo.ClassName);
            }

            WriteRuntimeTypeResolverBindings(connectionTypeInfo);

            _writer.WriteLine();
            _writer.WriteIndentedLine("Configure(descriptor);");
        }

        _writer.WriteIndentedLine("}");
    }

    private void WriteResolverBindings(IOutputTypeInfo typeInfo)
    {
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

                    if (resolver.Kind is ResolverKind.ConnectionResolver)
                    {
                        _writer.WriteIndentedLine(
                            ".AddPagingArguments()");
                        _writer.WriteIndentedLine(
                            ".Type<ObjectType<{0}>>()",
                            resolver.Member.GetReturnType()!.UnwrapTaskOrValueTask().ToFullyQualified());
                    }

                    _writer.WriteIndentedLine(".ExtendWith(c =>");
                    _writer.WriteIndentedLine("{");
                    using (_writer.IncreaseIndent())
                    {
                        WriteFieldFlags(resolver);

                        if(resolver.Kind is ResolverKind.ConnectionResolver)
                        {
                            _writer.WriteIndentedLine(
                                "var pagingOptions = global::{0}.GetPagingOptions(c.Context, null);",
                                WellKnownTypes.PagingHelper);
                            _writer.WriteIndentedLine(
                                "c.Definition.State = c.Definition.State.SetItem("
                                + "HotChocolate.WellKnownContextData.PagingOptions, pagingOptions);");
                            _writer.WriteIndentedLine(
                                "c.Definition.ContextData[HotChocolate.WellKnownContextData.PagingOptions] = "
                                + "pagingOptions;");
                        }

                        _writer.WriteIndentedLine(
                            "c.Definition.Resolvers = global::{0}.{1}Resolvers.{2}_{3}();",
                            typeInfo.Namespace,
                            typeInfo.ClassName,
                            typeInfo.ClassName,
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
    }

    private void WriteRuntimeTypeResolverBindings(IOutputTypeInfo typeInfo)
    {
        if (typeInfo.Resolvers.Length > 0)
        {
            foreach (var resolver in typeInfo.Resolvers)
            {
                _writer.WriteLine();
                _writer.WriteIndentedLine("descriptor");

                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine(
                        ".Field(runtimeType.GetMember(\"{0}\", runtimeBindingFlags)[0])",
                        resolver.Member.Name);

                    _writer.WriteIndentedLine(".ExtendWith(c =>");
                    _writer.WriteIndentedLine("{");
                    using (_writer.IncreaseIndent())
                    {
                        WriteFieldFlags(resolver);

                        _writer.WriteIndentedLine(
                            "c.Definition.Resolvers = global::{0}.{1}Resolvers.{2}_{3}();",
                            typeInfo.Namespace,
                            typeInfo.ClassName,
                            typeInfo.ClassName,
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
    }

    private void WriteFieldFlags(Resolver resolver)
    {
        _writer.WriteIndentedLine("c.Definition.SetSourceGeneratorFlags();");

        if (resolver.Kind is ResolverKind.ConnectionResolver)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionEdgesField) == FieldFlags.ConnectionEdgesField)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionEdgesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionNodesField) == FieldFlags.ConnectionNodesField)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionNodesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.TotalCount) == FieldFlags.TotalCount)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionTotalCountFieldFlags();");
        }
    }

    public void WriteConfigureMethod(IOutputTypeInfo typeInfo)
    {
        if (typeInfo.RuntimeType is null)
        {
            _writer.WriteIndentedLine(
                "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor descriptor);");
        }
        else
        {
            _writer.WriteIndentedLine(
                "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor);",
                typeInfo.RuntimeType.ToFullyQualified());
        }
    }
}
