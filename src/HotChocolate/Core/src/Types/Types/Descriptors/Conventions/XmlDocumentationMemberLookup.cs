using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors;

internal static class XmlDocumentationMemberLookup
{
    public static IReadOnlyDictionary<string, XElement> Create(XDocument document)
    {
        var memberLookup = new Dictionary<string, XElement>(StringComparer.Ordinal);
        var members = document.Element("doc")?.Element("members");
        if (members is null)
        {
            return memberLookup;
        }

        foreach (var member in members.Elements("member"))
        {
            var name = member.Attribute("name")?.Value;
            if (name is not null)
            {
                memberLookup.TryAdd(name, member);
            }
        }

        return memberLookup;
    }
}
