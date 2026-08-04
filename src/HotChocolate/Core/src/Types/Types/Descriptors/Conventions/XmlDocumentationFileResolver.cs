using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using IOPath = System.IO.Path;

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationFileResolver
    : IXmlDocumentationFileResolver
    , IXmlDocumentationResolver
{
    private const string Bin = "bin";

    private readonly Func<Assembly, string>? _resolveXmlDocumentationFileName;
    private readonly ConcurrentDictionary<string, Lazy<XmlDocumentation?>> _cache =
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
        var documentation = GetXmlDocumentation(assembly);
        document = documentation?.Document;
        return document is not null;
    }

    bool IXmlDocumentationResolver.TryGetMemberLookup(
        Assembly assembly,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, XElement>? memberLookup)
    {
        var documentation = GetXmlDocumentation(assembly);
        memberLookup = documentation?.MemberLookup;
        return memberLookup is not null;
    }

    private XmlDocumentation? GetXmlDocumentation(Assembly assembly)
    {
        var fullName = assembly.GetName().FullName!;

        if (!_cache.TryGetValue(fullName, out var documentation))
        {
            var candidate = new Lazy<XmlDocumentation?>(
                () => LoadXmlDocumentation(assembly),
                LazyThreadSafetyMode.ExecutionAndPublication);
            documentation = _cache.GetOrAdd(fullName, candidate);
        }

        try
        {
            return documentation.Value;
        }
        catch
        {
            _cache.TryRemove(fullName, out _);
            throw;
        }
    }

    private XmlDocumentation? LoadXmlDocumentation(Assembly assembly)
    {
        var xmlDocumentationFileName = GetXmlDocumentationPath(assembly);
        if (xmlDocumentationFileName is null || !File.Exists(xmlDocumentationFileName))
        {
            return null;
        }

        var document = XDocument.Load(xmlDocumentationFileName, LoadOptions.PreserveWhitespace);
        var memberLookup = XmlDocumentationMemberLookup.Create(document);

        return new XmlDocumentation(document, memberLookup);
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

            var expectedDocFile = _resolveXmlDocumentationFileName is null
                ? $"{assemblyName.Name}.xml"
                : _resolveXmlDocumentationFileName(assembly);

            string path;
#pragma warning disable IL3000 // Accessing Assembly.Location can return an empty string for assemblies embedded in a single-file app.
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                var assemblyDirectory = IOPath.GetDirectoryName(assembly.Location);
#pragma warning restore IL3000
                path = IOPath.Combine(assemblyDirectory!, expectedDocFile);
                if (File.Exists(path))
                {
                    return path;
                }
            }

#pragma warning disable SYSLIB0012
#pragma warning disable IL3002 // Accessing Assembly.CodeBase can cause issues in single-file and AOT publishing.
            var codeBase = assembly.CodeBase;
#pragma warning restore IL3002
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

    private sealed record XmlDocumentation(
        XDocument Document,
        IReadOnlyDictionary<string, XElement> MemberLookup);
}
