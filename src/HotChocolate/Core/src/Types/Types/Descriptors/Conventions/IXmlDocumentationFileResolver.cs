using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Resolves an XML documentation file from an assembly.
/// </summary>
public interface IXmlDocumentationFileResolver
{
    /// <summary>
    /// Trues to resolve an XML documentation file from the given assembly..
    /// </summary>
    bool TryGetXmlDocument(
        Assembly assembly,
        [NotNullWhen(true)] out XDocument? document);
}
