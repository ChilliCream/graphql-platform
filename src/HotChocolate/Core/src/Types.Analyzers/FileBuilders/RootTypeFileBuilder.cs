using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class RootTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteInitializeMethod(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type is not RootTypeInfo rootType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        Writer.WriteIndentedLine(
            "internal static void Initialize(global::{0} descriptor)",
            WellKnownTypes.IObjectTypeDescriptor);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            WriteInitializationBase(
                rootType.SchemaSchemaType.ToFullyQualified(),
                rootType.Resolvers.Length > 0,
                rootType.Resolvers.Any(t => t.RequiresParameterBindings),
                rootType.Attributes,
                rootType.Inaccessible);

            if (rootType.Shareable is DirectiveScope.Type)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Shareable);
            }

            WriteResolverBindings(rootType, typeLookup);

            Writer.WriteLine();
            Writer.WriteIndentedLine("Configure(descriptor);");
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }
}
