using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class InterfaceTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    protected override string OutputFieldDescriptorType => WellKnownTypes.InterfaceFieldDescriptor;

    public override void WriteInitializeMethod(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
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
            WriteInitializationBase(
                interfaceType.SchemaTypeFullName,
                interfaceType.Resolvers.Length > 0,
                interfaceType.Resolvers.Any(t => t.RequiresParameterBindings),
                interfaceType.DescriptorAttributes,
                interfaceType.Inaccessible);

            WriteResolverBindings(interfaceType, typeLookup);

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
