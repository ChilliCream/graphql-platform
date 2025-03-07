using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ConnectionTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not ConnectionTypeInfo connectionType)
        {
            throw new InvalidOperationException(
                "The specified type is not a connection type.");
        }

        Writer.WriteIndentedLine(
            "internal static void Initialize(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IObjectTypeDescriptor,
            connectionType.RuntimeTypeFullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            if (connectionType.Resolvers.Length > 0)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof(global::{0});",
                    connectionType.RuntimeTypeFullName);
                Writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    connectionType.Resolvers.Any(t => t.RequiresParameterBindings)
                        ? "var resolvers = new __Resolvers(bindingResolver);"
                        : "var resolvers = new __Resolvers();");
            }

            if (connectionType.RuntimeType.IsGenericType
                && !string.IsNullOrEmpty(connectionType.NameFormat))
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor");
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(
                        ".Name(t => string.Format(\"{0}\", t.Name));",
                        connectionType.NameFormat);
                    Writer.WriteIndentedLine(
                        ".DependsOn(typeof({0}));",
                        connectionType.RuntimeType.TypeArguments[0].ToFullyQualified());
                }
            }

            WriteResolverBindings(connectionType);

            Writer.WriteLine();
            Writer.WriteIndentedLine("Configure(descriptor);");
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }
}
