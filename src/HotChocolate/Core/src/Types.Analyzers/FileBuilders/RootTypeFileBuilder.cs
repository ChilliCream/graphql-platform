using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class RootTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    protected override string OutputFieldDescriptorType => WellKnownTypes.ObjectFieldDescriptor;

    public override void WriteBeginClass(IOutputTypeInfo type)
    {
        if (type is RootTypeInfo { IsStatic: false } rootType)
        {
            Writer.WriteIndentedLine(
                "{0} partial class {1}",
                rootType.IsPublic ? "public" : "internal",
                rootType.Name);
            Writer.WriteIndentedLine("{");
            Writer.IncreaseIndent();
            return;
        }

        base.WriteBeginClass(type);
    }

    protected override string GetInstanceReceiver(
        string fullyQualifiedTypeName,
        string contextExpression = "context")
        => $"{contextExpression}.Resolver<{fullyQualifiedTypeName}>()";

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
                rootType.SchemaTypeName.FullyQualifiedName,
                rootType.Resolvers.Length > 0,
                rootType.Resolvers.Any(t => t.RequiresParameterBindings),
                rootType.DescriptorAttributes,
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
