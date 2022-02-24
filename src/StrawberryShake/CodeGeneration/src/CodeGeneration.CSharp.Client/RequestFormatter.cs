using System.Runtime.Serialization;
using System.Xml.Linq;
using static System.Enum;
using static StrawberryShake.CodeGeneration.CSharp.Names;
using static StrawberryShake.CodeGeneration.CSharp.RequestOptions;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class RequestFormatter
{
    public static string Format(GeneratorRequest request)
    {
        var temp = GeneratorSink.Location;

        if (!Directory.Exists(temp))
        {
            Directory.CreateDirectory(temp);
        }

        var fileName = GeneratorSink.CreateFileName();

        var root = new XElement(Names.GeneratorRequest);
        root.Add(new XAttribute(ConfigFileName, request.ConfigFileName));
        root.Add(new XAttribute(RootDirectory, request.RootDirectory));
        root.Add(new XAttribute(DefaultNamespace, request.DefaultNamespace ?? string.Empty));
        root.Add(new XAttribute(PersistedQueryDirectory, request.PersistedQueryDirectory ?? string.Empty));
        root.Add(new XAttribute(Option, request.Option.ToString()));

        foreach (var file in request.DocumentFileNames)
        {
            root.Add(new XElement(DocumentFileName, file));
        }

        using FileStream fileStream = File.Create(fileName);
        var document = new XDocument();
        document.Add(root);
        document.Save(fileStream, SaveOptions.None);

        return fileName;
    }

    public static GeneratorRequest Take(string fileName)
    {
        GeneratorRequest request = Parse(fileName);
        File.Delete(fileName);
        return request;
    }

    public static GeneratorRequest Parse(string fileName)
    {
        using FileStream fileStream = File.OpenRead(fileName);
        var document = XDocument.Load(fileStream);

        if (document.Root is null || !document.Root.Name.LocalName.Equals(Names.GeneratorRequest))
        {
            throw new InvalidDataContractException("Missing the request root element.");
        }

        var configFileName = document.Root.Attribute(ConfigFileName)?.Value ??
            throw new InvalidDataContractException("Missing the configFileName attribute.");
        var rootDirectory = document.Root.Attribute(RootDirectory)?.Value;
        var defaultNamespace = document.Root.Attribute(DefaultNamespace)?.Value;
        var persistedQueryDirectory = document.Root.Attribute(PersistedQueryDirectory)?.Value;
        var optionString = document.Root.Attribute(Option)?.Value;

        var documentFileNames = new List<string>();

        foreach (XElement documentFileName in document.Root.Elements(DocumentFileName))
        {
            if (!string.IsNullOrEmpty(documentFileName.Value))
            {
                documentFileNames.Add(documentFileName.Value);
            }
        }

        return new GeneratorRequest(
            configFileName,
            documentFileNames,
            string.IsNullOrEmpty(rootDirectory) ? null : rootDirectory,
            string.IsNullOrEmpty(defaultNamespace) ? null : defaultNamespace,
            string.IsNullOrEmpty(persistedQueryDirectory) ? null : persistedQueryDirectory,
            string.IsNullOrEmpty(optionString) ? Default : Parse<RequestOptions>(optionString));
    }
}
