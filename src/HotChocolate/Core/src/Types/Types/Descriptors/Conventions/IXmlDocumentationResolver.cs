using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors;

internal interface IXmlDocumentationResolver
{
    bool TryGetMemberLookup(
        Assembly assembly,
        [NotNullWhen(true)] out IReadOnlyDictionary<string, XElement>? memberLookup);
}
