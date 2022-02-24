using System.Runtime.Serialization;
using System.Xml.Linq;
using static StrawberryShake.CodeGeneration.CSharp.Names;

namespace StrawberryShake.CodeGeneration.CSharp;

public static class ResponseFormatter
{
    public static void Format(GeneratorResponse response, string fileName)
    {
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        using FileStream fileStream = File.Create(fileName);
        var document = new XDocument();
        SerializeGeneratorResponse(document, response);
        document.Save(fileStream, SaveOptions.None);
    }

    public static GeneratorResponse Take(string fileName)
    {
        GeneratorResponse response = Parse(fileName);
        File.Delete(fileName);
        return response;
    }

    public static GeneratorResponse Parse(string fileName)
    {
        using FileStream fileStream = File.OpenRead(fileName);
        var document = XDocument.Load(fileStream);
        return DeserializeGeneratorResponse(document);
    }

    private static GeneratorResponse DeserializeGeneratorResponse(XDocument document)
    {
        if (document.Root is null || !document.Root.Name.LocalName.Equals(Names.GeneratorResponse))
        {
            throw new InvalidDataContractException("The generator response has an invalid format.");
        }

        var documents = new List<GeneratorDocument>();
        var errors = new List<GeneratorError>();

        foreach (XElement documentElement in document.Root.Elements(Document))
        {
            documents.Add(DeserializeGeneratorDocument(documentElement));
        }

        foreach (XElement errorElement in document.Root.Elements(Error))
        {
            errors.Add(DeserializeGeneratorError(errorElement));
        }

        return new GeneratorResponse(documents, errors);
    }

    private static void SerializeGeneratorResponse(XDocument parent, GeneratorResponse response)
    {
        var responseElement = new XElement(Names.GeneratorResponse);

        foreach (GeneratorDocument document in response.Documents)
        {
            SerializeGeneratorDocument(responseElement, document);
        }

        foreach (GeneratorError error in response.Errors)
        {
            SerializeGeneratorError(responseElement, error);
        }

        parent.Add(responseElement);
    }

    private static GeneratorDocument DeserializeGeneratorDocument(XElement documentElement)
    {
        XAttribute? nameAttribute = documentElement.Attribute(Name);
        XAttribute? kindAttribute = documentElement.Attribute(Kind);
        XElement? sourceTextElement = documentElement.Element(SourceText);
        XAttribute? hashAttribute = documentElement.Attribute(Hash);
        XAttribute? pathAttribute = documentElement.Attribute(Names.Path);

        if (nameAttribute is null || kindAttribute is null || sourceTextElement is null)
        {
            throw new InvalidDataContractException("The document has an invalid format.");
        }

        return new GeneratorDocument(
            nameAttribute.Value,
            sourceTextElement.Value,
            Enum.Parse<GeneratorDocumentKind>(kindAttribute.Value),
            hashAttribute?.Value,
            pathAttribute?.Value);
    }

    private static void SerializeGeneratorDocument(XElement parent, GeneratorDocument document)
    {
        var documentElement = new XElement(Document);
        documentElement.Add(new XAttribute(Name, document.Name));
        documentElement.Add(new XAttribute(Kind, document.Kind.ToString()));
        documentElement.Add(new XElement(SourceText, document.SourceText));

        if (document.Hash is not null)
        {
            documentElement.Add(new XAttribute(Hash, document.Hash));
        }

        if (document.Path is not null)
        {
            documentElement.Add(new XAttribute(Names.Path, document.Path));
        }

        parent.Add(documentElement);
    }

    private static GeneratorError DeserializeGeneratorError(XElement errorElement)
    {
        XAttribute? codeAttribute = errorElement.Attribute(Code);
        XAttribute? titleAttribute = errorElement.Attribute(Title);
        XAttribute? filePathAttribute = errorElement.Attribute(FilePath);
        XElement? messageElement = errorElement.Element(Message);
        XElement? locationElement = errorElement.Element(ErrorLocation);

        if (messageElement is null || codeAttribute is null || titleAttribute is null)
        {
            throw new InvalidDataContractException("The error has an invalid format.");
        }

        Location? location = null;
        if (locationElement is not null)
        {
            location = DeserializeLocation(locationElement);
        }

        return new GeneratorError(
            codeAttribute.Value,
            titleAttribute.Value,
            messageElement.Value,
            filePathAttribute?.Value,
            location);
    }

    private static void SerializeGeneratorError(XElement parent, GeneratorError error)
    {
        var errorElement = new XElement(Error);
        errorElement.Add(new XAttribute(Code, error.Code));
        errorElement.Add(new XAttribute(Title, error.Title));

        if (!string.IsNullOrEmpty(error.FilePath))
        {
            errorElement.Add(new XAttribute(FilePath, error.FilePath));
        }

        errorElement.Add(new XElement(Message, error.Message));

        if (error.Location is not null)
        {
            SerializeLocation(errorElement, error.Location);
        }

        parent.Add(errorElement);
    }

    private static Location DeserializeLocation(XElement location)
    {
        XAttribute? line = location.Attribute(Line);
        XAttribute? column = location.Attribute(Column);

        return new Location(
            line is null ? 0 : int.Parse(line.Value),
            column is null ? 0 : int.Parse(column.Value));
    }

    private static void SerializeLocation(XElement parent, Location location)
    {
        var locationElement = new XElement(ErrorLocation);
        locationElement.Add(new XAttribute(Line, location.Line));
        locationElement.Add(new XAttribute(Column, location.Column));
        parent.Add(locationElement);
    }
}
