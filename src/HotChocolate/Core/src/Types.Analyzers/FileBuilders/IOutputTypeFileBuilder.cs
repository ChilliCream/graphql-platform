using HotChocolate.Types.Analyzers.Models;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public interface IOutputTypeFileBuilder
{
    void WriteHeader();

    void WriteBeginNamespace();
    void WriteEndNamespace();

    string WriteBeginClass(string typeName);
    void WriteEndClass();

    void WriteInitializeMethod(IOutputTypeInfo typeInfo);

    void WriteConfigureMethod(IOutputTypeInfo typeInfo);
}
