using System.Collections.Immutable;
using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

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

    public bool AddResolverDeclarations(ImmutableArray<Resolver> resolvers)
    {
        var first = true;

        foreach (var resolver in resolvers)
        {
            if (resolver.Parameters.Length == 0)
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
                resolver.TypeName,
                resolver.Member.Name,
                resolver.Parameters.Length);
        }

        return !first;
    }

    public void AddParameterInitializer(IEnumerable<Resolver> resolvers, ILocalTypeLookup typeLookup)
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
                if (resolver.Parameters.Length == 0)
                {
                    continue;
                }

                if (resolver.Member is not IMethodSymbol method)
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
                    _writer.WriteLine();
                    _writer.WriteIndentedLine("var type = typeof({0});", method.ContainingType.ToFullyQualified());
                    _writer.WriteIndentedLine("global::System.Reflection.MethodInfo resolver = default!;");
                    _writer.WriteIndentedLine("global::System.Reflection.ParameterInfo[] parameters = default!;");
                    first = false;
                }

                _writer.WriteLine();
                _writer.WriteIndentedLine("resolver = type.GetMethod(");
                using (_writer.IncreaseIndent())
                {
                    _writer.WriteIndentedLine("\"{0}\",", resolver.Member.Name);
                    _writer.WriteIndentedLine("bindingFlags,");
                    if (resolver.Parameters.Length == 0)
                    {
                        _writer.WriteIndentedLine("global::System.Array.Empty<global::System.Type>());");
                    }
                    else
                    {
                        _writer.WriteIndentedLine("new global::System.Type[]");
                        _writer.WriteIndentedLine("{");
                        using (_writer.IncreaseIndent())
                        {
                            for (var i = 0; i < resolver.Parameters.Length; i++)
                            {
                                var parameter = resolver.Parameters[i];

                                if (i > 0)
                                {
                                    _writer.Write(',');
                                    _writer.WriteLine();
                                }

                                _writer.WriteIndented(
                                    "typeof({0})",
                                    ToFullyQualifiedString(parameter.Type, method, typeLookup));
                            }
                        }

                        _writer.WriteLine();
                        _writer.WriteIndentedLine("});");
                    }
                }

                _writer.WriteIndentedLine("parameters = resolver.GetParameters();");

                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    _writer.WriteIndentedLine(
                        "_args_{0}_{1}[{2}] = bindingResolver.GetBinding(parameters[{2}]);",
                        resolver.TypeName,
                        resolver.Member.Name,
                        i);
                }
            }
        }

        _writer.WriteIndentedLine("}");
    }

    private static string ToFullyQualifiedString(
        ITypeSymbol type,
        IMethodSymbol resolverMethod,
        ILocalTypeLookup typeLookup)
    {
        if (type.TypeKind is TypeKind.Error &&
            typeLookup.TryGetTypeName(type, resolverMethod, out var typeDisplayName))
        {
            return typeDisplayName;
        }

        return type.ToFullyQualified();
    }

    public void AddResolver(Resolver resolver, ILocalTypeLookup typeLookup)
    {
        if (resolver.Member is IMethodSymbol resolverMethod)
        {
            switch (resolver.ResultKind)
            {
                case ResolverResultKind.Invalid:
                    return;

                case ResolverResultKind.Pure when resolver.IsPure:
                    AddStaticPureResolver(resolver, resolverMethod, typeLookup);
                    return;

                case ResolverResultKind.Pure when !resolver.IsPure:
                    AddStaticStandardResolver(resolver, false, resolverMethod, typeLookup);
                    return;

                case ResolverResultKind.Task:
                case ResolverResultKind.TaskAsyncEnumerable:
                    AddStaticStandardResolver(resolver, true, resolverMethod, typeLookup);
                    return;

                case ResolverResultKind.Executable:
                case ResolverResultKind.Queryable:
                case ResolverResultKind.AsyncEnumerable:
                    AddStaticStandardResolver(resolver, false, resolverMethod, typeLookup);
                    return;
            }
        }

        AddStaticPropertyResolver(resolver);
    }

    private void AddStaticStandardResolver(
        Resolver resolver,
        bool async,
        IMethodSymbol resolverMethod,
        ILocalTypeLookup typeLookup)
    {
        _writer.WriteIndented("public static ");

        if (async)
        {
            _writer.Write("async ");
        }

        _writer.WriteLine(
            "global::{0}<global::{1}?> {2}_{3}(global::{4} context)",
            WellKnownTypes.ValueTask,
            WellKnownTypes.Object,
            resolver.TypeName,
            resolver.Member.Name,
            WellKnownTypes.ResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            AddResolverArguments(resolver, resolverMethod, typeLookup);

            if (async)
            {
                _writer.WriteIndentedLine(
                    "var result = await {0}.{1}({2});",
                    resolver.Member.ContainingType.ToFullyQualified(),
                    resolver.Member.Name,
                    GetResolverArguments(resolver.Parameters.Length));

                _writer.WriteIndentedLine("return result;");
            }
            else
            {
                _writer.WriteIndentedLine(
                    "var result = {0}.{1}({2});",
                    resolver.Member.ContainingType.ToFullyQualified(),
                    resolver.Member.Name,
                    GetResolverArguments(resolver.Parameters.Length));

                _writer.WriteIndentedLine(
                    "return new global::{0}<global::{1}?>(result);",
                    WellKnownTypes.ValueTask,
                    WellKnownTypes.Object);
            }
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPureResolver(Resolver resolver, IMethodSymbol resolverMethod, ILocalTypeLookup typeLookup)
    {
        _writer.WriteIndentedLine(
            "public static global::{0}? {1}_{2}(global::{3} context)",
            WellKnownTypes.Object,
            resolver.TypeName,
            resolver.Member.Name,
            WellKnownTypes.PureResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            AddResolverArguments(resolver, resolverMethod, typeLookup);

            _writer.WriteIndentedLine(
                "var result = {0}.{1}({2});",
                resolver.Member.ContainingType.ToFullyQualified(),
                resolver.Member.Name,
                GetResolverArguments(resolver.Parameters.Length));

            _writer.WriteIndentedLine("return result;");
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPropertyResolver(Resolver resolver)
    {
        _writer.WriteIndentedLine(
            "public static global::{0}? {1}_{2}(global::{3} context)",
            WellKnownTypes.Object,
            resolver.TypeName,
            resolver.Member.Name,
            WellKnownTypes.PureResolverContext);
        _writer.WriteIndentedLine("{");
        using (_writer.IncreaseIndent())
        {
            _writer.WriteIndentedLine(
                "var result = {0}.{1};",
                resolver.Member.ContainingType.ToFullyQualified(),
                resolver.Member.Name);

            _writer.WriteIndentedLine("return result;");
        }

        _writer.WriteIndentedLine("}");
    }

    private void AddResolverArguments(Resolver resolver, IMethodSymbol resolverMethod, ILocalTypeLookup typeLookup)
    {
        if (resolver.Parameters.Length <= 0)
        {
            return;
        }

        for (var i = 0; i < resolver.Parameters.Length; i++)
        {
            var parameter = resolver.Parameters[i];

            switch (parameter.Kind)
            {
                case ResolverParameterKind.Parent:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Parent<{1}>();",
                        i,
                        resolver.Parameters[i].Type.ToFullyQualified());
                    break;

                case ResolverParameterKind.CancellationToken:
                    _writer.WriteIndentedLine("var args{0} = context.RequestAborted;", i);
                    break;
                case ResolverParameterKind.ClaimsPrincipal:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<{1}>(\"ClaimsPrincipal\");",
                        i,
                        WellKnownTypes.ClaimsPrincipal);
                    break;
                case ResolverParameterKind.DocumentNode:
                    _writer.WriteIndentedLine("var args{0} = context.Operation.Document;", i);
                    break;
                case ResolverParameterKind.EventMessage:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedState<{1}>(" +
                        "global::HotChocolate.WellKnownContextData.EventMessage);",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;
                case ResolverParameterKind.FieldNode:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Selection.SyntaxNode",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;
                case ResolverParameterKind.OutputField:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Selection.Field",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;
                case ResolverParameterKind.HttpContext:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;
                case ResolverParameterKind.HttpRequest:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Request!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;
                case ResolverParameterKind.HttpResponse:
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Response!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;
                case ResolverParameterKind.GetGlobalState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalStateOrDefault<{1}>(\"{2}\", {3});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key,
                        defaultValueString);
                    break;
                }
                case ResolverParameterKind.GetGlobalState when !parameter.IsNullable:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.GetGlobalState:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.SetGlobalState:
                    _writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>(" +
                        "value => context.SetGlobalState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;
                case ResolverParameterKind.GetScopedState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedStateOrDefault<{1}>(\"{2}\", {3});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key,
                        defaultValueString);
                    break;
                }
                case ResolverParameterKind.GetScopedState when !parameter.IsNullable:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.GetScopedState:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.SetScopedState:
                    _writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>(" +
                        "value => context.SetScopedState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;
                case ResolverParameterKind.GetLocalState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = ConvertDefaultValueToString(defaultValue, parameter.Type);

                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalStateOrDefault<{1}>(\"{2}\", {3});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key,
                        defaultValueString);
                    break;
                }
                case ResolverParameterKind.GetLocalState when !parameter.IsNullable:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.GetLocalState:
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;
                }
                case ResolverParameterKind.SetLocalState:
                    _writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>(" +
                        "value => context.SetLocalState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.Service:
                case ResolverParameterKind.Argument:
                case ResolverParameterKind.Unknown:
                    _writer.WriteIndentedLine(
                        "var args{0} = _args_{1}_{2}[{0}].Execute<{3}>(context);",
                        i,
                        resolver.TypeName,
                        resolver.Member.Name,
                        ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private string GetResolverArguments(int parameterCount)
    {
        if (parameterCount == 0)
        {
            return string.Empty;
        }

        var arguments = new StringBuilder();

        for (var i = 0; i < parameterCount; i++)
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
