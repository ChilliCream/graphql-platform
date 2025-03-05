using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ObjectTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not ObjectTypeExtensionInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        Writer.WriteIndentedLine(
            "internal static void Initialize(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IObjectTypeDescriptor,
            objectType.RuntimeTypeFullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            if (objectType.Resolvers.Length > 0 || objectType.NodeResolver is not null)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof({0});",
                    objectType.SchemaSchemaType.ToFullyQualified());
                Writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    "var resolvers = new __Resolvers(bindingResolver);");
            }

            if (objectType.NodeResolver is not null)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor");
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(".ImplementsNode()");
                    Writer.WriteIndentedLine(
                        ".ResolveNode(resolvers.{0}().Resolver!);",
                        objectType.NodeResolver.Member.Name);
                }
            }

            WriteResolverBindings(objectType);

            Writer.WriteLine();
            Writer.WriteIndentedLine("Configure(descriptor);");
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }
}
