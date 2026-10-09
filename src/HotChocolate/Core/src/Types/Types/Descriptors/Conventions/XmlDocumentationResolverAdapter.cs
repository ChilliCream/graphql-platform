using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors;

internal sealed class XmlDocumentationResolverAdapter(
    IXmlDocumentationFileResolver fileResolver)
    : IXmlDocumentationResolver
{
    private readonly ConditionalWeakTable<XDocument, IReadOnlyDictionary<string, XElement>> _cache = new();

    public bool TryGetMemberLookup(
        Assembly assembly,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, XElement>? memberLookup)
    {
        if (!fileResolver.TryGetXmlDocument(assembly, out var document))
        {
            memberLookup = null;
            return false;
        }

        memberLookup = _cache.GetValue(document, CreateMemberLookup);
        return true;
    }

    private static IReadOnlyDictionary<string, XElement> CreateMemberLookup(XDocument document) =>
        XmlDocumentationMemberLookup.Create(document);
}
