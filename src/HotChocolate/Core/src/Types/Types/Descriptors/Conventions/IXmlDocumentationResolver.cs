using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Resolves an XML documentation file from an assembly.
/// </summary>
public interface IXmlDocumentationResolver
{
    /// <summary>
    /// Trues to resolve an XML documentation element from the given assembly..
    /// </summary>
    bool TryGetXmlDocument(Assembly assembly,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, XElement>? memberLookup);
}
