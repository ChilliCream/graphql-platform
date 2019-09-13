using System.Reflection;
using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors
{
    public interface IXmlDocumentationFileResolver
    {
        bool TryGetXmlDocument(Assembly assembly, out XDocument document);
    }
}
