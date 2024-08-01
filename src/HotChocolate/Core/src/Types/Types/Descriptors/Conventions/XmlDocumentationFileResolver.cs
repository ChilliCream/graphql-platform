using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using IOPath = System.IO.Path;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationFileResolver : IXmlDocumentationFileResolver
{
    private const string _bin = "bin";

    private readonly Func<Assembly, string>? _resolveXmlDocumentationFileName;

    private readonly ConcurrentDictionary<string, XDocument> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public XmlDocumentationFileResolver()
    {
        _resolveXmlDocumentationFileName = null;
    }

    public XmlDocumentationFileResolver(Func<Assembly, string>? resolveXmlDocumentationFileName)
    {
        _resolveXmlDocumentationFileName = resolveXmlDocumentationFileName;
    }

    public bool TryGetXmlDocument(
        Assembly assembly,
        [NotNullWhen(true)] out XDocument? document)
    {
        var fullName = assembly.GetName().FullName;

        if (!_cache.TryGetValue(fullName, out var doc))
        {
            var xmlDocumentFileName = GetXmlDocumentationPath(assembly);

            if (xmlDocumentFileName is not null && File.Exists(xmlDocumentFileName))
            {
                doc = XDocument.Load(xmlDocumentFileName, LoadOptions.PreserveWhitespace);
                _cache[fullName] = doc;
            }
        }

        document = doc;
        return document != null;
    }

    private string? GetXmlDocumentationPath(Assembly? assembly)
    {
        try
        {
            if (assembly is null)
            {
                return null;
            }

            var assemblyName = assembly.GetName();
            if (string.IsNullOrEmpty(assemblyName.Name))
            {
                return null;
            }

            if (_cache.ContainsKey(assemblyName.FullName))
            {
                return null;
            }

            var expectedDocFile = _resolveXmlDocumentationFileName is null
                ? $"{assemblyName.Name}.xml"
                : _resolveXmlDocumentationFileName(assembly);

            string path;
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                var assemblyDirectory = IOPath.GetDirectoryName(assembly.Location);
                path = IOPath.Combine(assemblyDirectory!, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }
            }

#pragma warning disable SYSLIB0012
            var codeBase = assembly.CodeBase;
#pragma warning restore SYSLIB0012
            if (!string.IsNullOrEmpty(codeBase))
            {
                path = IOPath.Combine(
                    IOPath.GetDirectoryName(codeBase.Replace("file:///", string.Empty))!,
                    expectedDocFile)
                    .Replace("file:\\", string.Empty);

                if (File.Exists(path))
                {
                    return path;
                }
            }

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                path = IOPath.Combine(baseDirectory, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }

                return IOPath.Combine(baseDirectory, _bin, expectedDocFile);
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            path = IOPath.Combine(currentDirectory, expectedDocFile);
            if (File.Exists(path))
            {
                return path;
            }

            path = IOPath.Combine(currentDirectory, _bin, expectedDocFile);

            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
