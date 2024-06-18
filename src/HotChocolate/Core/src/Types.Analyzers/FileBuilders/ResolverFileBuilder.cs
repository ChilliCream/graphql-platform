using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ResolverFileBuilder(StringBuilder sb)
{
    private readonly CodeWriter _writer = new(sb);

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using HotChocolate.Internal;");
        _writer.WriteLine();
    }

    public void WriteBeginNamespace(string ns)
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
        _writer.WriteIndentedLine("internal static class {0}", typeName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
        _writer.WriteIndentedLine("private static bool _bindingsInitialized;");
        return typeName;
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public bool AddResolverDeclarations(IEnumerable<ResolverInfo> resolvers)
    {
        var first = true;

        foreach (var resolver in resolvers)
        {
            if (resolver.Skip)
            {
                continue;
            }

            if (!first)
            {
                _writer.WriteLine();
            }

            first = false;

            _writer.WriteIndentedLine(
                "private readonly static global::{0}[] _args_{1}_{2} = new global::{0}[{3}];",
                WellKnownTypes.ParameterBinding,
                resolver.Name.TypeName,
                resolver.Name.MemberName,
                resolver.ParameterCount);
        }

        return !first;
    }

    public void AddParameterInitializer(IEnumerable<ResolverInfo> resolvers, ILocalTypeLookup typeLookup)
    {
        _writer.WriteIndentedLine(
            "public static void InitializeBindings(global::{0} bindingResolver)",
            WellKnownTypes.ParameterBindingResolver);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            var first = true;
            foreach (var resolver in resolvers)
            {
                if (resolver is not { Skip: false, Method: not null })
                {
                    continue;
                }

                if (first)
                {
                    _writer.WriteIndentedLine("if (_bindingsInitialized)");
                    _writer.WriteIndentedLine("{");
                    using (_writer.IncreaseIndent())
                    {
                        _writer.WriteIndentedLine("return;");
                    }

                    _writer.WriteIndentedLine("}");
                    _writer.WriteIndentedLine("_bindingsInitialized = true;");
                    _writer.WriteLine();
                    _writer.WriteIndentedLine(
                        "const global::{0} bindingFlags =",
                        WellKnownTypes.BindingFlags);
                    _writer.WriteIndentedLine(
                        "    global::{0}.Public",
                        WellKnownTypes.BindingFlags);
                    _writer.WriteIndentedLine(
                        "        | global::{0}.NonPublic",
                        WellKnownTypes.BindingFlags);
                    _writer.WriteIndentedLine(
                        "        | global::{0}.Static;",
                        WellKnownTypes.BindingFlags);
                    _writer.WriteIndentedLine(
                        "var type = typeof(global::{0});",
                        resolver.Method.ContainingType.ToDisplayString());
                    first = false;
                }

                _writer.WriteLine();
                _writer.WriteIndentedLine(
                    "var resolver_{0}_{1} = type.GetMethod(\"{1}\", bindingFlags, [{2}])!;",
                    resolver.Name.TypeName,
                    resolver.Name.MemberName,
                    string.Join(", ", resolver.Method.Parameters.Select(
                        p => $"typeof(global::{ToDisplayString(p.Type, resolver, typeLookup)})")));

                _writer.WriteIndentedLine(
                    "var parameters_{0}_{1} = resolver_{0}_{1}.GetParameters();",
                    resolver.Name.TypeName,
                    resolver.Name.MemberName);

                for (var i = 0; i < resolver.Method.Parameters.Length; i++)
                {
                    _writer.WriteIndentedLine(
                        "_args_{0}_{1}[{2}] = bindingResolver.GetBinding(parameters_{0}_{1}[{2}]);",
                        resolver.Name.TypeName,
                        resolver.Name.MemberName,
                        i);
                }
            }
        }

        _writer.WriteIndentedLine("}");
    }

    private static string ToDisplayString(ITypeSymbol type, ResolverInfo resolver, ILocalTypeLookup typeLookup)
    {
        if (type.TypeKind is TypeKind.Error &&
            typeLookup.TryGetTypeName(type, resolver, out var typeDisplayName))
        {
            return typeDisplayName;
        }

        return type.ToDisplayString();
    }

    public void AddResolver(ResolverName resolverName, ISymbol member, Compilation compilation)
    {
        if (member is IMethodSymbol method)
        {
            switch (method.GetResultKind())
            {
                case ResolverResultKind.Invalid:
                    return;

                case ResolverResultKind.Pure:
                    AddStaticPureResolver(resolverName, method, compilation);
                    return;

                case ResolverResultKind.Task:
                case ResolverResultKind.TaskAsyncEnumerable:
                    AddStaticStandardResolver(resolverName, method, true, compilation);
                    return;

                case ResolverResultKind.Executable:
                case ResolverResultKind.Queryable:
                case ResolverResultKind.AsyncEnumerable:
                    AddStaticStandardResolver(resolverName, method, false, compilation);
                    return;
            }
        }

        AddStaticPropertyResolver(resolverName, member);
    }

    private void AddStaticStandardResolver(
        ResolverName resolverName,
        IMethodSymbol method,
        bool async,
        Compilation compilation)
    {
        _writer.WriteIndented("public static ");

        if (async)
        {
            _writer.Write("async ");
        }

        _writer.WriteLine(
            "global::{0}<{1}?> {2}_{3}(global::{4} context)",
            WellKnownTypes.ValueTask,
            WellKnownTypes.Object,
            resolverName.TypeName,
            resolverName.MemberName,
            WellKnownTypes.ResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            AddResolverArguments(resolverName, method, compilation);

            if (async)
            {
                _writer.WriteIndentedLine(
                    "var result = await {0}.{1}({2});",
                    method.ContainingType.ToFullyQualified(),
                    resolverName.MemberName,
                    GetResolverArguments(method));

                _writer.WriteIndentedLine(
                    "return result;",
                    WellKnownTypes.ValueTask,
                    WellKnownTypes.Object);
            }
            else
            {
                _writer.WriteIndentedLine(
                    "var result = {0}.{1}({2});",
                    method.ContainingType.ToFullyQualified(),
                    resolverName.MemberName,
                    GetResolverArguments(method));

                _writer.WriteIndentedLine(
                    "return new global::{0}<{1}?>(result);",
                    WellKnownTypes.ValueTask,
                    WellKnownTypes.Object);
            }
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPureResolver(ResolverName resolverName, IMethodSymbol method, Compilation compilation)
    {
        _writer.WriteIndentedLine(
            "public static {0}? {1}_{2}(global::{3} context)",
            WellKnownTypes.Object,
            resolverName.TypeName,
            resolverName.MemberName,
            WellKnownTypes.PureResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            AddResolverArguments(resolverName, method, compilation);

            _writer.WriteIndentedLine(
                "var result = {0}.{1}({2});",
                method.ContainingType.ToFullyQualified(),
                resolverName.MemberName,
                GetResolverArguments(method));

            _writer.WriteIndentedLine("return result;");
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPropertyResolver(ResolverName resolverName, ISymbol method)
    {
        _writer.WriteIndentedLine(
            "public static {0}? {1}_{2}(global::{3} context)",
            WellKnownTypes.Object,
            resolverName.TypeName,
            resolverName.MemberName,
            WellKnownTypes.PureResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "var result = {0}.{1};",
                method.ContainingType.ToFullyQualified(),
                resolverName.MemberName);

            _writer.WriteIndentedLine(
                "return result;",
                WellKnownTypes.ValueTask,
                WellKnownTypes.Object);
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddResolverArguments(ResolverName resolverName, IMethodSymbol method, Compilation compilation)
    {
        if (method.Parameters.Length > 0)
        {
            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var parameter = method.Parameters[i];

                if (parameter.IsParent())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Parent<{1}>();",
                        i,
                        method.Parameters[i].Type.ToFullyQualified());
                }
                else if (parameter.IsCancellationToken())
                {
                    _writer.WriteIndentedLine("var args{0} = context.RequestAborted;", i);
                }
                else if (parameter.IsClaimsPrincipal())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<{1}>(\"ClaimsPrincipal\");",
                        i,
                        WellKnownTypes.ClaimsPrincipal);
                }
                else if (parameter.IsDocumentNode())
                {
                    _writer.WriteIndentedLine("var args{0} = context.Operation.Document;", i);
                }
                else if (parameter.IsEventMessage())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedState<{1}>(" +
                        "global::HotChocolate.WellKnownContextData.EventMessage);",
                        i,
                        parameter.Type.ToFullyQualified());
                }
                else if (parameter.IsFieldNode())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Selection.SyntaxNode",
                        i,
                        parameter.Type.ToFullyQualified());
                }
                else if (parameter.IsOutputField(compilation))
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Selection.Field",
                        i,
                        parameter.Type.ToFullyQualified());
                }
                else if (parameter.IsHttpContext())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))!;",
                        i,
                        WellKnownTypes.HttpContext);
                }
                else if (parameter.IsHttpRequest())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Request!;",
                        i,
                        WellKnownTypes.HttpContext);
                }
                else if (parameter.IsHttpResponse())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Response!;",
                        i,
                        WellKnownTypes.HttpContext);
                }
                else if (parameter.IsGlobalState(out var key))
                {
                    if (parameter.IsSetState(out var stateTypeName))
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = new HotChocolate.SetState<{1}>(" +
                            "value => context.SetGlobalState(\"{2}\", value));",
                            i,
                            stateTypeName,
                            key);
                    }
                    else if (parameter.HasExplicitDefaultValue)
                    {
                        var defaultValue = parameter.ExplicitDefaultValue;

                        var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetGlobalStateOrDefault<{1}>(\"{2}\", {3});",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key,
                            defaultValueString);
                    }
                    else if (parameter.IsNonNullable())
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetGlobalState<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                    else
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetGlobalStateOrDefault<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                }
                else if (parameter.IsScopedState(out key))
                {
                    if (parameter.IsSetState(out var stateTypeName))
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = new HotChocolate.SetState<{1}>(" +
                            "value => context.SetScopedState(\"{2}\", value));",
                            i,
                            stateTypeName,
                            key);
                    }
                    else if (parameter.HasExplicitDefaultValue)
                    {
                        var defaultValue = parameter.ExplicitDefaultValue;

                        var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetScopedStateOrDefault<{1}>(\"{2}\", {3});",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key,
                            defaultValueString);
                    }
                    else if (parameter.IsNonNullable())
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetScopedState<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                    else
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetScopedStateOrDefault<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                }
                else if (parameter.IsLocalState(out key))
                {
                    if (parameter.IsSetState(out var stateTypeName))
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = new HotChocolate.SetState<{1}>(" +
                            "value => context.SetLocalState(\"{2}\", value));",
                            i,
                            stateTypeName,
                            key);
                    }
                    else if (parameter.HasExplicitDefaultValue)
                    {
                        var defaultValue = parameter.ExplicitDefaultValue;

                        var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetLocalStateOrDefault<{1}>(\"{2}\", {3});",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key,
                            defaultValueString);
                    }
                    else if (parameter.IsNonNullable())
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetLocalState<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                    else
                    {
                        _writer.WriteIndentedLine(
                            "var args{0} = context.GetLocalStateOrDefault<{1}>(\"{2}\");",
                            i,
                            parameter.Type.ToFullyQualified(),
                            key);
                    }
                }
                else
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = _args_{1}_{2}[{0}].Execute<{3}>(context);",
                        i,
                        resolverName.TypeName,
                        resolverName.MemberName,
                        method.Parameters[i].Type.ToFullyQualified());
                }
            }
        }
    }

    private string GetResolverArguments(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return string.Empty;
        }

        var arguments = new StringBuilder();

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            if (i > 0)
            {
                arguments.Append(", ");
            }

            arguments.Append($"args{i}");
        }

        return arguments.ToString();
    }

    private static string ConvertDefaultValueToString(object? defaultValue, ITypeSymbol type)
    {
        if (defaultValue == null)
        {
            return "null";
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return $"\"{defaultValue}\"";
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            return $"'{defaultValue}'";
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            return defaultValue.ToString().ToLower();
        }

        if (type.SpecialType == SpecialType.System_Double ||
            type.SpecialType == SpecialType.System_Single)
        {
            return $"{defaultValue}d";
        }

        if (type.SpecialType == SpecialType.System_Decimal)
        {
            return $"{defaultValue}m";
        }

        if (type.SpecialType == SpecialType.System_Int64 ||
            type.SpecialType == SpecialType.System_UInt64)
        {
            return $"{defaultValue}L";
        }

        return defaultValue.ToString();
    }
}

public interface ILocalTypeLookup
{
    bool TryGetTypeName(ITypeSymbol type, ResolverInfo resolver, [NotNullWhen(true)] out string? typeDisplayName);
}

public sealed class DefaultLocalTypeLookup(ImmutableArray<ISyntaxInfo> syntaxInfos) : ILocalTypeLookup
{
    private Dictionary<string, List<string>>? _typeNameLookup;

    public bool TryGetTypeName(ITypeSymbol type, ResolverInfo resolver, [NotNullWhen(true)] out string? typeDisplayName)
    {
        var typeNameLookup = GetTypeNameLookup();

        if(!typeNameLookup.TryGetValue(type.Name, out var typeNames))
        {
            typeDisplayName = null;
            return false;
        }

        if(typeNames.Count == 1)
        {
            typeDisplayName = typeNames[0];
            return true;
        }

        if (resolver.Method is not null)
        {
            foreach (var namespaceString in GetContainingNamespaces(resolver.Method))
            {
                if (typeNames.Contains($"{namespaceString}.{type.Name}"))
                {
                    typeDisplayName = typeNames[0];
                    return true;
                }
            }
        }

        typeDisplayName = type.Name;
        return true;
    }

    private Dictionary<string, List<string>> GetTypeNameLookup()
    {
        if (_typeNameLookup is null)
        {
            _typeNameLookup = new Dictionary<string, List<string>>();
            foreach (var dataLoaderInfo in syntaxInfos.OfType<DataLoaderInfo>())
            {
                if (!_typeNameLookup.TryGetValue(dataLoaderInfo.Name, out var typeNames))
                {
                    typeNames = [];
                    _typeNameLookup[dataLoaderInfo.Name] = typeNames;
                }

                typeNames.Add(dataLoaderInfo.FullName);

                if (!_typeNameLookup.TryGetValue(dataLoaderInfo.InterfaceName, out typeNames))
                {
                    typeNames = [];
                    _typeNameLookup[dataLoaderInfo.InterfaceName] = typeNames;
                }

                typeNames.Add(dataLoaderInfo.InterfaceFullName);
            }
        }

        return _typeNameLookup;
    }

    private IEnumerable<string> GetContainingNamespaces(IMethodSymbol methodSymbol)
    {
        var namespaces = new HashSet<string>();
        var syntaxTree = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;

        if (syntaxTree != null)
        {
            var root = syntaxTree.GetRoot();
            var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

            foreach (var namespaceDeclaration in namespaceDeclarations)
            {
                namespaces.Add(namespaceDeclaration.Name.ToString());
            }
        }

        return namespaces;
    }
}
