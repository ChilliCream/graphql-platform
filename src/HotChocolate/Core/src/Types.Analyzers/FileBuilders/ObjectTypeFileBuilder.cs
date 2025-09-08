using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public sealed class ObjectTypeFileBuilder(StringBuilder sb) : TypeFileBuilderBase(sb)
{
    public override void WriteInitializeMethod(IOutputTypeInfo type)
    {
        if (type is not ObjectTypeInfo objectType)
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
                    "var thisType = typeof(global::{0});",
                    objectType.SchemaTypeFullName);
                Writer.WriteIndentedLine(
                    "var bindingResolver = descriptor.Extend().Context.ParameterBindingResolver;");
                Writer.WriteIndentedLine(
                    objectType.Resolvers.Any(t => t.RequiresParameterBindings)
                        || (objectType.NodeResolver?.RequiresParameterBindings ?? false)
                        ? "var resolvers = new __Resolvers(bindingResolver);"
                        : "var resolvers = new __Resolvers();");
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
            $"global::{objectType.SchemaTypeFullName}",
            type.Resolvers.Any(t => t.RequiresParameterBindings)
            || (objectType.NodeResolver?.RequiresParameterBindings ?? false));
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
                WriteResolverBindingInitialization(objectType.NodeResolver, typeLookup);
            }
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
