using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public abstract class TypeFileBuilderBase(StringBuilder sb)
{
    private readonly CodeWriter _writer = new(sb);

    public void WriteHeader()
    {
        _writer.WriteFileHeader();
        _writer.WriteIndentedLine("using HotChocolate.Internal;");
        _writer.WriteLine();
    }

    public void WriteBeginNamespace(string @namespace)
    {
        _writer.WriteIndentedLine("namespace {0}", @namespace);
        _writer.WriteIndentedLine("{");
        _writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
        _writer.WriteLine();
    }

    public string WriteBeginClass(string typeName)
    {
        _writer.WriteIndentedLine("internal static partial class {0}", typeName);
        _writer.WriteIndentedLine("{");
        return typeName;
    }

    public void WriteEndClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public abstract void WriteInitializeMethod(IOutputTypeInfo type);

    protected void WriteFieldFlags(Resolver resolver)
    {
        _writer.WriteIndentedLine("c.Definition.SetSourceGeneratorFlags();");

        if (resolver.Kind is ResolverKind.ConnectionResolver)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionEdgesField) == FieldFlags.ConnectionEdgesField)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionEdgesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionNodesField) == FieldFlags.ConnectionNodesField)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionNodesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.TotalCount) == FieldFlags.TotalCount)
        {
            _writer.WriteIndentedLine("c.Definition.SetConnectionTotalCountFieldFlags();");
        }
    }

    public abstract void WriteConfigureMethod(IOutputTypeInfo type);

    public string WriteBeginResolverClass(string typeName)
    {
        _writer.WriteIndentedLine("private sealed class __Resolvers");
        _writer.WriteIndentedLine("{");
        return typeName;
    }

    public void WriteEndResolverClass()
    {
        _writer.DecreaseIndent();
        _writer.WriteIndentedLine("}");
    }

    public void WriteParameterBindingFields(IOutputTypeInfo type)
    {
        throw new NotImplementedException();
    }

    public void WriteResolverClassConstructor(IOutputTypeInfo type)
    {
        throw new NotImplementedException();
    }

    public void WriteResolverMethods(IOutputTypeInfo type)
    {
        throw new NotImplementedException();
    }
}
