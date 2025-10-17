using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationResolver : IXmlDocumentationResolver
{
    private const string Bin = "bin";

    private readonly Func<Assembly, string>? _resolveXmlDocumentationFileName;

    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, XElement>?> _cache =
        new(StringComparer.Ordinal);

    public XmlDocumentationResolver()
    {
        _resolveXmlDocumentationFileName = null;
    }

    public XmlDocumentationResolver(Func<Assembly, string>? resolveXmlDocumentationFileName)
    {
        _resolveXmlDocumentationFileName = resolveXmlDocumentationFileName;
    }

    public bool TryGetXmlDocument(
        Assembly assembly,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, XElement>? memberLookup)
    {
        var fullName = assembly.GetName().FullName;

        if (!_cache.TryGetValue(fullName, out memberLookup))
        {
            var xmlDocumentFileName = GetXmlDocumentationPath(assembly);
            if (xmlDocumentFileName is not null && File.Exists(xmlDocumentFileName))
            {
                var doc = XDocument.Load(xmlDocumentFileName, LoadOptions.PreserveWhitespace);
                memberLookup =
                    doc.Element("doc")?
                        .Element("members")?
                        .Elements("member")
                        .Where(static x => x.Attribute("name") != null)
                        .ToDictionary(static x => x.Attribute("name")!.Value, static delegate(XElement x)
                        {
                            // Optimize memory usage: We already stored the name as key in the dictionary.
                            x.RemoveAttributes();
                            return x;
                        });
            }

            _cache.TryAdd(fullName, memberLookup);
        }

        return memberLookup != null;
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

                return IOPath.Combine(baseDirectory, Bin, expectedDocFile);
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            path = IOPath.Combine(currentDirectory, expectedDocFile);
            if (File.Exists(path))
            {
                return path;
            }

            path = IOPath.Combine(currentDirectory, Bin, expectedDocFile);

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
