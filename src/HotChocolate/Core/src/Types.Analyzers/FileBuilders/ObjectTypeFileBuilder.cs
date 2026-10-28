using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ObjectTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    protected override string OutputFieldDescriptorType => WellKnownTypes.ObjectFieldDescriptor;

    public override void WriteInitializeMethod(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type is not ObjectTypeInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        Writer.WriteIndentedLine(
            "internal static void Initialize(global::{0}<global::{1}> descriptor)",
            WellKnownTypes.IObjectTypeDescriptor,
            objectType.RuntimeTypeName.FullName);

        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            WriteInitializationBase(
                objectType.SchemaTypeName.FullName,
                objectType.Resolvers.Length > 0 || objectType.NodeResolver is not null,
                objectType.Resolvers.Any(t => t.RequiresParameterBindings)
                    || (objectType.NodeResolver?.RequiresParameterBindings ?? false),
                objectType.DescriptorAttributes,
                objectType.Inaccessible);

            if (objectType.Shareable is DirectiveScope.Type)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Shareable);
            }

            if (objectType.NodeResolver is not null)
            {
                WriteNodeResolver(objectType.NodeResolver, typeLookup);
            }

            WriteResolverBindings(objectType, typeLookup);

            Writer.WriteLine();
            Writer.WriteIndentedLine("Configure(descriptor);");
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    private void WriteNodeResolver(Resolver nodeResolver, ILocalTypeLookup typeLookup)
    {
        Writer.WriteLine();

        if (nodeResolver.DescriptorAttributes.Length == 0)
        {
            Writer.WriteIndentedLine("descriptor");
            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine(".ImplementsNode()");
                Writer.WriteIndentedLine(
                    ".ResolveNode(resolvers.{0}().Resolver!);",
                    nodeResolver.Member.Name);
            }

            return;
        }

        Writer.WriteIndentedLine("var nodeDescriptor = descriptor.ImplementsNode();");
        Writer.WriteIndentedLine(
            "nodeDescriptor.ResolveNode(resolvers.{0}().Resolver!);",
            nodeResolver.Member.Name);

        // the node resolver is not a field of the type, so its descriptor attributes
        // are applied to the field configuration from which the node resolver pipeline is compiled.
        Writer.WriteLine();
        Writer.WriteIndentedLine(
            "var nodeResolverConfiguration = ((global::{0})nodeDescriptor).Extend().Configuration.ResolverField!;",
            WellKnownTypes.NodeDescriptorBase);
        Writer.WriteIndentedLine("nodeResolverConfiguration.Member = thisType.GetMethod(");
        WriteGetMethodArguments(nodeResolver, typeLookup);

        Writer.WriteLine();
        Writer.WriteIndentedLine(
            "var nodeResolverDescriptor = global::{0}.From(extension.Context, nodeResolverConfiguration);",
            OutputFieldDescriptorType);
        Writer.WriteIndentedLine(
            "{0}.ApplyConfiguration(",
            WellKnownTypes.ConfigurationHelper);
        using (Writer.IncreaseIndent())
        {
            Writer.WriteIndentedLine("extension.Context,");
            Writer.WriteIndentedLine("nodeResolverDescriptor,");
            Writer.WriteIndentedLine("nodeResolverConfiguration.Member,");

            var first = true;

            foreach (var attribute in nodeResolver.DescriptorAttributes)
            {
                if (!first)
                {
                    Writer.WriteLine(',');
                }

                Writer.WriteIndent();
                Writer.Write(GenerateAttributeInstantiation(attribute));
                first = false;
            }

            Writer.WriteLine([')', ';']);
        }

        Writer.WriteIndentedLine("nodeResolverConfiguration.ConfigurationsAreApplied = true;");
        Writer.WriteIndentedLine("nodeResolverDescriptor.CreateConfiguration();");
    }

    public override void WriteResolverFields(IOutputTypeInfo type)
    {
        if (type is not ObjectTypeInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        base.WriteResolverFields(objectType);

        if (objectType.NodeResolver is not null)
        {
            if (objectType.NodeResolver.RequiresParameterBindings)
            {
                WriteResolverField(objectType.NodeResolver);
            }

            WriteIsSelectedFields(objectType.NodeResolver);
        }
    }

    public override void WriteResolverConstructor(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type is not ObjectTypeInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        WriteResolverConstructor(
            objectType,
            typeLookup,
            type.Resolvers.Any(t => t.RequiresParameterBindings)
            || (objectType.NodeResolver?.RequiresParameterBindings ?? false),
            type.Resolvers.Any(HasIsSelectedFields)
            || HasIsSelectedFields(objectType.NodeResolver));
    }

    protected override void WriteResolversBindingInitialization(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type is not ObjectTypeInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        base.WriteResolversBindingInitialization(objectType, typeLookup);

        if (objectType.NodeResolver is not null)
        {
            if (objectType.NodeResolver.RequiresParameterBindings)
            {
                WriteResolverBindingInitialization(objectType.NodeResolver);
            }

            WriteIsSelectedInitialization(objectType.NodeResolver);
        }
    }

    public override void WriteResolverMethods(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type is not ObjectTypeInfo objectType)
        {
            throw new InvalidOperationException(
                "The specified type is not an object type.");
        }

        base.WriteResolverMethods(objectType, typeLookup);

        if (objectType.NodeResolver is not null)
        {
            if (objectType.Resolvers.Length > 0)
            {
                Writer.WriteLine();
            }

            WriteResolver(objectType.NodeResolver, typeLookup);
        }
    }
}
