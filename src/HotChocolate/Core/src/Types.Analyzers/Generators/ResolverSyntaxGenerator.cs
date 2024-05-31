using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class ResolverSyntaxGenerator(StringBuilder sb, string ns)
{
    private readonly CodeWriter _writer = new(sb);

    public void WriterHeader()
    {
        _writer.WriteFileHeader();
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
        _writer.WriteIndentedLine("internal static class {0}", typeName);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
        return typeName;
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void AddResolverDeclarations(IEnumerable<ResolverInfo> resolvers)
    {
        foreach (var resolver in resolvers)
        {
            if(resolver.Skip)
            {
                continue;
            }

            _writer.WriteIndentedLine(
                "private readonly static global::{0}[] _args_{1}_{2} = new global::{0}[{3}];",
                WellKnownTypes.ParameterBinding,
                resolver.Name.TypeName,
                resolver.Name.MemberName,
                resolver.ParameterCount);
        }
    }

    public void AddResolver(ResolverName resolverName, ISymbol member)
    {
        if (member is IMethodSymbol method)
        {
            switch (method.GetResultKind())
            {
                case ResolverResultKind.Invalid:
                    return;

                case ResolverResultKind.Pure:
                    AddStaticPureResolver(resolverName, method);
                    return;

                case ResolverResultKind.Task:
                case ResolverResultKind.TaskAsyncEnumerable:
                    AddStaticStandardResolver(resolverName, method, true);
                    return;

                case ResolverResultKind.Executable:
                case ResolverResultKind.Queryable:
                case ResolverResultKind.AsyncEnumerable:
                    AddStaticStandardResolver(resolverName, method, false);
                    return;
            }
        }

        AddStaticPropertyResolver(resolverName, member);
    }

    private void AddStaticStandardResolver(ResolverName resolverName, IMethodSymbol method, bool async)
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
            AddResolverArguments(resolverName, method);

            if (async)
            {
                _writer.WriteIndentedLine(
                    "var result = await {0}.{1}({2});",
                    method.ContainingType.ToFullyQualified(),
                    resolverName.MemberName,
                    GetResolverArguments(resolverName, method));

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
                    GetResolverArguments(resolverName, method));

                _writer.WriteIndentedLine(
                    "return new global::{0}<{1}?>(result);",
                    WellKnownTypes.ValueTask,
                    WellKnownTypes.Object);
            }

        }
        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPureResolver(ResolverName resolverName, IMethodSymbol method)
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
            AddResolverArguments(resolverName, method);

            _writer.WriteIndentedLine(
                "var result = {0}.{1}({2});",
                method.ContainingType.ToFullyQualified(),
                resolverName.MemberName,
                GetResolverArguments(resolverName, method));

            _writer.WriteIndentedLine("return result;");
        }
        _writer.WriteIndentedLine("}");
    }

    private void AddStaticPropertyResolver(ResolverName resolverName, ISymbol method)
    {
        _writer.WriteIndentedLine(
            "private static {0}? {1}_{2}(global::{3} context)",
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

    private void AddResolverArguments(ResolverName resolverName, IMethodSymbol method)
    {
        if (method.Parameters.Length > 0)
        {
            if(method.Parameters.Length > 1 || !method.Parameters[0].IsParent())
            {
                _writer.WriteIndentedLine(
                    "var args = global::{0}.GetReference(_args_{1}_{2}.AsSpan());",
                    WellKnownTypes.MemoryMarshal,
                    resolverName.TypeName,
                    resolverName.MemberName);
            }


            for (var i = 0; i < method.Parameters.Length; i++)
            {
                var parameter = method.Parameters[i];

                if (parameter.IsParent())
                {
                    _writer.WriteIndentedLine(
                        "var args{0} = context.Parent<{1}>();",
                        i,
                        method.Parameters[i].Type.ToFullyQualified());
                    continue;
                }

                _writer.WriteIndentedLine(
                    "var args{0} = global::{1}.Add(ref args, {0}).Execute<global::{2}>(context);",
                    i,
                    WellKnownTypes.Unsafe,
                    method.Parameters[i].Type.ToFullyQualified());
            }
        }
    }

    private string GetResolverArguments(ResolverName resolverName, IMethodSymbol method)
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
}
