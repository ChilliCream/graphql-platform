using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class InterfaceTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not InterfaceTypeInfo interfaceType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        Writer.WriteIndentedLine(
            "internal static void Initialize(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IInterfaceTypeDescriptor,
            interfaceType.RuntimeTypeFullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            if (interfaceType.Resolvers.Length > 0)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof({0});",
                    interfaceType.SchemaSchemaType.ToFullyQualified());
                Writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    interfaceType.Resolvers.Any(t => t.RequiresParameterBindings)
                            ? "var resolvers = new __Resolvers(bindingResolver);"
                            : "var resolvers = new __Resolvers();");
            }

            WriteResolverBindings(interfaceType);

            Writer.WriteLine();
            Writer.WriteIndentedLine("Configure(descriptor);");
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    public override void WriteConfigureMethod(IOutputTypeInfo type)
    {
        if (type is not InterfaceTypeInfo interfaceType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        Writer.WriteIndentedLine(
            "static partial void Configure(global::{0}<global::{1}> descriptor);",
            WellKnownTypes.IInterfaceTypeDescriptor,
            interfaceType.RuntimeTypeFullName);
        Writer.WriteLine();
    }
}
