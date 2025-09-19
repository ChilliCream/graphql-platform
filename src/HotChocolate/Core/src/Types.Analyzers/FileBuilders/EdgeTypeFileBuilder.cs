using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class EdgeTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteBeginClass(IOutputTypeInfo type)
    {
        Writer.WriteIndentedLine(
            "{0} partial class {1} : ObjectType<global::{2}>",
            type.IsPublic ? "public" : "internal",
            type.Name,
            type.RuntimeTypeFullName);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not EdgeTypeInfo edgeType)
        {
            throw new InvalidOperationException(
                "The specified type is not an edge type.");
        }

        Writer.WriteIndentedLine(
            "protected override void Configure(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IObjectTypeDescriptor,
            edgeType.RuntimeTypeFullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            if (edgeType.Resolvers.Length > 0)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof(global::{0});",
                    edgeType.RuntimeTypeFullName);
                Writer.WriteIndentedLine(
                    "var extend = descriptor.Extend();");
                Writer.WriteIndentedLine(
                    "var bindingResolver = extend.Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    edgeType.Resolvers.Any(t => t.RequiresParameterBindings)
                        ? "var resolvers = new __Resolvers(bindingResolver);"
                        : "var resolvers = new __Resolvers();");
            }

            if (edgeType.RuntimeType.IsGenericType
                && !string.IsNullOrEmpty(edgeType.NameFormat)
                && edgeType.NameFormat!.Contains("{0}"))
            {
                var nodeTypeName = edgeType.RuntimeType.TypeArguments[0].ToFullyQualified();
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "var nodeTypeRef = extend.Context.TypeInspector.GetTypeRef(typeof({0}));",
                    nodeTypeName);
                Writer.WriteIndentedLine("descriptor");
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(
                        ".Name(t => string.Format(\"{0}\", t.Name))",
                        edgeType.NameFormat);
                    Writer.WriteIndentedLine(
                        ".DependsOn(nodeTypeRef);");
                }
            }
            else if (!string.IsNullOrEmpty(edgeType.NameFormat))
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "descriptor.Name(\"{0}\");",
                    edgeType.NameFormat);
            }

            WriteResolverBindings(edgeType);
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    public override void WriteConfigureMethod(IOutputTypeInfo type)
    {
    }
}
