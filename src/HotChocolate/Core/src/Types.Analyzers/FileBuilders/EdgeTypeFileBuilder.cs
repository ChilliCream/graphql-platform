using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class EdgeTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    protected override string OutputFieldDescriptorType => WellKnownTypes.ObjectFieldDescriptor;

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

    public override void WriteInitializeMethod(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
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
            if (edgeType.Resolvers.Length > 0 || edgeType.DescriptorAttributes.Length > 0)
            {
                Writer.WriteIndentedLine("var extension = descriptor.Extend();");
                Writer.WriteIndentedLine("var configuration = extension.Configuration;");
            }

            if (edgeType.Resolvers.Length > 0)
            {
                Writer.WriteIndentedLine(
                    "var thisType = typeof(global::{0});",
                    edgeType.RuntimeTypeFullName);
                Writer.WriteIndentedLine(
                    "var bindingResolver = extension.Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    edgeType.Resolvers.Any(t => t.RequiresParameterBindings)
                        ? "var resolvers = new __Resolvers(bindingResolver);"
                        : "var resolvers = new __Resolvers();");
            }

            if (edgeType.DescriptorAttributes.Length > 0)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "{0}.ApplyConfiguration(",
                    WellKnownTypes.ConfigurationHelper);
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine("extension.Context,");
                    Writer.WriteIndentedLine("descriptor,");
                    Writer.WriteIndentedLine("null,");

                    var first = true;
                    foreach (var attribute in edgeType.DescriptorAttributes)
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
            }

            if (edgeType.Inaccessible is DirectiveScope.Type)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Inaccessible);
            }

            if (edgeType.Shareable is DirectiveScope.Type)
            {
                Writer.WriteLine();
                Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Shareable);
            }
            else
            {
                Writer.WriteLine();
                using (Writer.WriteIfClause("extension.Context.Options.ApplyShareableToConnections"))
                {
                    Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Shareable);
                }
            }

            if (edgeType.RuntimeType.IsGenericType
                && !string.IsNullOrEmpty(edgeType.NameFormat)
                && edgeType.NameFormat!.Contains("{0}"))
            {
                var nodeTypeName = edgeType.RuntimeType.TypeArguments[0].ToFullyQualified();
                Writer.WriteLine();
                Writer.WriteIndentedLine(
                    "var nodeTypeRef = extension.Context.TypeInspector.GetTypeRef(typeof({0}));",
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

            WriteResolverBindings(edgeType, typeLookup);
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    public override void WriteConfigureMethod(IOutputTypeInfo type)
    {
    }
}
